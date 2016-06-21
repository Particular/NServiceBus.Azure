namespace NServiceBus.SagaPersisters.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using global::NServiceBus.Azure;
    using Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using SecondaryIndeces;
    using Saga;

    /// <summary>
    /// Saga persister implementation using azure table storage.
    /// </summary>
    public class AzureSagaPersister : ISagaPersister
    {
        readonly bool autoUpdateSchema;
        readonly CloudTableClient client;
        readonly SecondaryIndexPersister secondaryIndeces;
        readonly ILog log = LogManager.GetLogger<AzureSagaPersister>();

        static readonly ConcurrentDictionary<string, bool> tableCreated = new ConcurrentDictionary<string, bool>();
        static readonly ConditionalWeakTable<object, string> etags = new ConditionalWeakTable<object, string>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <param name="autoUpdateSchema"></param>
        public AzureSagaPersister(CloudStorageAccount account, bool autoUpdateSchema)
        {
            this.autoUpdateSchema = autoUpdateSchema;
            client = account.CreateCloudTableClient();
            secondaryIndeces = new SecondaryIndexPersister(GetTable, ScanForSaga, saga => Persist(saga, null));
        }

        /// <summary>
        /// Saves the given saga entity using the current session of the
        /// injected session factory.
        /// </summary>
        /// <param name="saga">the saga entity that will be saved.</param>
        public void Save(IContainSagaData saga)
        {
            // The following operations have to be executed sequentially:
            // 1) insert the 2nd index, containing the primary saga data (just in case of a failure)
            // 2) insert the primary saga data in its row, storing the identifier of the secondary index as well (for completions)
            // 3) remove the data of the primary from the 2nd index. It will be no longer needed

            var secondaryIndexKey = secondaryIndeces.Insert(saga);
            Persist(saga, secondaryIndexKey);
            secondaryIndeces.MarkAsHavingPrimaryPersisted(saga);
        }
        
        /// <summary>
        /// Updates the given saga entity using the current session of the
        /// injected session factory.
        /// </summary>
        /// <param name="saga">the saga entity that will be updated.</param>
        public void Update(IContainSagaData saga)
        {
            Persist(saga, null);
        }

        /// <summary>
        /// Gets a saga entity from the injected session factory's current session
        /// using the given saga id.
        /// </summary>
        /// <param name="sagaId">The saga id to use in the lookup.</param>
        /// <returns>The saga entity if found, otherwise null.</returns>
        public T Get<T>(Guid sagaId) where T : IContainSagaData
        {
            var id = sagaId.ToString();
            var entityType = typeof(T);
            var tableEntity = GetDictionaryTableEntity(id, entityType);
            var entity = (T)ToEntity(entityType, tableEntity);

            if (!Equals(entity, default(T)))
            {
                etags.Add(entity, tableEntity.ETag);
                EntityProperty value;
                if (tableEntity.TryGetValue(SecondaryIndexIndicatorProperty, out value))
                {
                    secondaryIndexLocalCache.Add(entity, PartitionRowKeyTuple.Parse(value.StringValue));
                }
            }

            return entity;
        }

        DictionaryTableEntity GetDictionaryTableEntity(string sagaId, Type entityType)
        {
            var tableName = entityType.Name;
            var table = client.GetTableReference(tableName);

            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaId));

            var tableEntity = table.ExecuteQuery(query).SafeFirstOrDefault();
            return tableEntity;
        }

        T ISagaPersister.Get<T>(string property, object value)
        {
            var sagaId = secondaryIndeces.FindPossiblyCreatingIndexEntry<T>(property, value);
            if (sagaId != null)
            {
                return Get<T>(sagaId.Value);
            }

            return default(T);
        }

        DictionaryTableEntity GetDictionaryTableEntity(Type type, string property, object value)
        {
            var tableName = type.Name;
            var table = client.GetTableReference(tableName);

            var query = BuildWherePropertyQuery(type, property, value);
            var tableEntity = table.ExecuteQuery(query).SafeFirstOrDefault();
            return tableEntity;
        }

        private static TableQuery<DictionaryTableEntity> BuildWherePropertyQuery(Type type, string property, object value)
        {
            TableQuery<DictionaryTableEntity> query;

            var propertyInfo = type.GetProperty(property);

            if (propertyInfo.PropertyType == typeof(byte[]))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBinary(property, QueryComparisons.Equal, (byte[]) value));
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBool(property, QueryComparisons.Equal, (bool) value));
            }
            else if (propertyInfo.PropertyType == typeof(DateTime))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDate(property, QueryComparisons.Equal, (DateTime) value));
            }
            else if (propertyInfo.PropertyType == typeof(Guid))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForGuid(property, QueryComparisons.Equal, (Guid) value));
            }
            else if (propertyInfo.PropertyType == typeof(Int32))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForInt(property, QueryComparisons.Equal, (int) value));
            }
            else if (propertyInfo.PropertyType == typeof(Int64))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForLong(property, QueryComparisons.Equal, (long) value));
            }
            else if (propertyInfo.PropertyType == typeof(Double))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDouble(property, QueryComparisons.Equal, (double) value));
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition(property, QueryComparisons.Equal, (string) value));
            }
            else
            {
                throw new NotSupportedException(
                    string.Format("The property type '{0}' is not supported in windows azure table storage",
                        propertyInfo.PropertyType.Name));
            }
            return query;
        }

        /// <summary>
        /// Deletes the given saga from the injected session factory's
        /// current session.
        /// </summary>
        /// <param name="saga">The saga entity that will be deleted.</param>
        public void Complete(IContainSagaData saga)
        {
            var tableName = saga.GetType().Name;
            var table = client.GetTableReference(tableName);

            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, saga.Id.ToString()));

            var entity = table.ExecuteQuery(query).SafeFirstOrDefault();
            if (entity == null)
            {
                return; // should not try to delete saga data that does not exist, this situation can occur on retry or parallel execution
            }

            table.DeleteIgnoringNotFound(entity);
            try
            {
                RemoveSecondaryIndex(saga);
            }
            catch
            {
                log.Warn($"Removal of the secondary index entry for the following saga failed: '{saga.Id}'");
            }
        }

        void RemoveSecondaryIndex(IContainSagaData sagaData)
        {
            PartitionRowKeyTuple secondaryIndexKey;
            if (secondaryIndexLocalCache.TryGetValue(sagaData, out secondaryIndexKey))
            {
                secondaryIndeces.RemoveSecondary(sagaData.GetType(), secondaryIndexKey);
            }
        }

        void Persist(IContainSagaData saga, PartitionRowKeyTuple secondaryIndexKey)
        {
            var type = saga.GetType();
            var table = GetTable(type);

            var partitionKey = saga.Id.ToString();

            var batch = new TableBatchOperation();

            AddObjectToBatch(batch, saga, partitionKey, secondaryIndexKey);

            table.ExecuteBatch(batch);
        }

        private CloudTable GetTable(Type sagaType)
        {
            var tableName = sagaType.Name;
            var table = client.GetTableReference(tableName);
            if (autoUpdateSchema && !tableCreated.ContainsKey(tableName))
            {
                table.CreateIfNotExists();
                tableCreated[tableName] = true;
            }
            return table;
        }

        private Guid[] ScanForSaga(Type sagaType, string propertyName, object propertyValue)
        {
            var query = BuildWherePropertyQuery(sagaType, propertyName, propertyValue);
            query.SelectColumns = new List<string>
            {
                "PartitionKey",
                "RowKey"
            };

            var tableName = sagaType.Name;
            var table = client.GetTableReference(tableName);
            var entities = table.ExecuteQuery(query);
            return entities.Select(entity => Guid.ParseExact(entity.PartitionKey, "D")).ToArray();
        }

        void AddObjectToBatch(TableBatchOperation batch, object entity, string partitionKey, PartitionRowKeyTuple secondaryIndexKey, string rowkey = "")
        {
            if (rowkey == "")
            {
                // just to be backward compat with original implementation
                rowkey = partitionKey;
            }

            var type = entity.GetType();
            string etag;

            var update = etags.TryGetValue(entity, out etag);

            if (secondaryIndexKey == null && update)
            {
                secondaryIndexLocalCache.TryGetValue(entity, out secondaryIndexKey);
            }

            var properties = SelectPropertiesToPersist(type);

            var toPersist = ToDictionaryTableEntity(entity, new DictionaryTableEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowkey,
                ETag = etag
            }, properties);

            if (secondaryIndexKey != null)
            {
                toPersist.Add(SecondaryIndexIndicatorProperty, secondaryIndexKey.ToString());
            }

            //no longer using InsertOrReplace as it ignores concurrency checks
            batch.Add(update ? TableOperation.Replace(toPersist) : TableOperation.Insert(toPersist));
        }

        internal static PropertyInfo[] SelectPropertiesToPersist(Type sagaType)
        {
            return sagaType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        DictionaryTableEntity ToDictionaryTableEntity(object entity, DictionaryTableEntity toPersist, IEnumerable<PropertyInfo> properties)
        {
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType == typeof(byte[]))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((byte[])propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(bool))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((bool)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(DateTime))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((DateTime)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Guid))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Guid)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Int32))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Int32)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Int64))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Int64)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Double))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Double)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(string))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((string)propertyInfo.GetValue(entity, null));
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format("The property type '{0}' is not supported in windows azure table storage",
                                      propertyInfo.PropertyType.Name));
                }
            }
            return toPersist;
        }

        object ToEntity(Type entityType, DictionaryTableEntity entity)
        {
            if (entity == null) return null;

            var toCreate = Activator.CreateInstance(entityType);
            foreach (var propertyInfo in entityType.GetProperties())
            {
                if (entity.ContainsKey(propertyInfo.Name))
                {
                    if (propertyInfo.PropertyType == typeof(byte[]))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].BinaryValue, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(bool))
                    {
                        var boolean = entity[propertyInfo.Name].BooleanValue;
                        propertyInfo.SetValue(toCreate, boolean.HasValue && boolean.Value, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(DateTime))
                    {
                        var dateTimeOffset = entity[propertyInfo.Name].DateTimeOffsetValue;
                        propertyInfo.SetValue(toCreate, dateTimeOffset.HasValue ? dateTimeOffset.Value.DateTime : default(DateTime), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        var guid = entity[propertyInfo.Name].GuidValue;
                        propertyInfo.SetValue(toCreate, guid.HasValue ? guid.Value : default(Guid), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        var int32 = entity[propertyInfo.Name].Int32Value;
                        propertyInfo.SetValue(toCreate, int32.HasValue ? int32.Value : default(Int32), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Double))
                    {
                        var d = entity[propertyInfo.Name].DoubleValue;
                        propertyInfo.SetValue(toCreate, d.HasValue ? d.Value : default(Int64), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Int64))
                    {
                        var int64 = entity[propertyInfo.Name].Int64Value;
                        propertyInfo.SetValue(toCreate, int64.HasValue ? int64.Value : default(Int64), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(string))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].StringValue, null);
                    }
                    else
                    {
                        throw new NotSupportedException(
                            string.Format("The property type '{0}' is not supported in windows azure table storage",
                                propertyInfo.PropertyType.Name));
                    }
                }
            }
            return toCreate;
        }
        const string SecondaryIndexIndicatorProperty = "NServiceBus_2ndIndexKey";
        static ConditionalWeakTable<object, PartitionRowKeyTuple> secondaryIndexLocalCache = new ConditionalWeakTable<object, PartitionRowKeyTuple>();
    }


}
