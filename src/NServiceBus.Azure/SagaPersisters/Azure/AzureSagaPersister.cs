namespace NServiceBus.SagaPersisters.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Reflection;
    using System.Runtime.Caching;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.Azure;
    using Saga;

    /// <summary>
    /// Saga persister implementation using azure table storage.
    /// </summary>
    public class AzureSagaPersister : ISagaPersister
    {
        readonly bool autoUpdateSchema;
        readonly CloudTableClient client;
        readonly ConcurrentDictionary<string, bool> tableCreated = new ConcurrentDictionary<string, bool>();
        static readonly MemoryCache dictionaryTableCache = new MemoryCache("Entities");
        const int longevity = 60000;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <param name="autoUpdateSchema"></param>
        public AzureSagaPersister(CloudStorageAccount account, bool autoUpdateSchema)
        {
            this.autoUpdateSchema = autoUpdateSchema;
            client = account.CreateCloudTableClient();
        }

        /// <summary>
        /// Saves the given saga entity using the current session of the
        /// injected session factory.
        /// </summary>
        /// <param name="saga">the saga entity that will be saved.</param>
        public void Save(IContainSagaData saga)
        {
            Persist(saga);
        }

        /// <summary>
        /// Updates the given saga entity using the current session of the
        /// injected session factory.
        /// </summary>
        /// <param name="saga">the saga entity that will be updated.</param>
        public void Update(IContainSagaData saga)
        {
            Persist(saga);
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
                AddToCache(id, tableEntity);
            }

            return entity;
        }

        void AddToCache(string id, DictionaryTableEntity tableEntity)
        {
            var item = dictionaryTableCache.GetCacheItem(id);
            if (item == null)
            {
                item = new CacheItem(id, tableEntity);
                dictionaryTableCache.Set(item, new CacheItemPolicy
                {
                    Priority = CacheItemPriority.NotRemovable,
                    SlidingExpiration = TimeSpan.FromMilliseconds(longevity)
                });
            }
            else
            {
                item.Value = tableEntity;
            }
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
            var type = typeof(T);
            var tableEntity = GetDictionaryTableEntity(type, property, value);
            var entity = (T) ToEntity(type, tableEntity);

            if (!Equals(entity, default(T)))
            {
                var id = entity.Id.ToString();
                AddToCache(id, tableEntity);
            }

            try
            {
                return entity;
            }
            catch (WebException ex)
            {
                // can occur when table has not yet been created, but already looking for absence of instance
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var response = (HttpWebResponse) ex.Response;
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return default(T);
                    }
                }

                throw;
            }
            catch (StorageException)
            {
                // can occur when table has not yet been created, but already looking for absence of instance
                return default(T);
            }
        }

        DictionaryTableEntity GetDictionaryTableEntity(Type type, string property, object value)
        {
            var tableName = type.Name;
            var table = client.GetTableReference(tableName);

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
            var tableEntity = table.ExecuteQuery(query).SafeFirstOrDefault();
            return tableEntity;
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

            table.Execute(TableOperation.Delete(entity));
        }

        void Persist(IContainSagaData saga)
        {
            var type = saga.GetType();
            var tableName = type.Name;
            var table = client.GetTableReference(tableName);
            if (autoUpdateSchema && !tableCreated.ContainsKey(tableName))
            {
                table.CreateIfNotExists();
                tableCreated[tableName] = true;
            }

            var partitionKey = saga.Id.ToString();
                
            var batch = new TableBatchOperation();

            AddObjectToBatch(batch, saga, partitionKey);

            table.ExecuteBatch(batch);
        }

        void AddObjectToBatch(TableBatchOperation batch, object entity, string partitionKey, string rowkey = "")
        {
            if (rowkey == "") rowkey = partitionKey; // just to be backward compat with original implementation

            var type = entity.GetType();
            var cacheItem = dictionaryTableCache.GetCacheItem(partitionKey);
            var toPersist = cacheItem != null ? (DictionaryTableEntity)(cacheItem.Value) : new DictionaryTableEntity { PartitionKey = partitionKey, RowKey = rowkey };
                
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            toPersist = ToDictionaryTableEntity(entity, toPersist, properties);

            var update = !string.IsNullOrEmpty(toPersist.ETag);

            //no longer using InsertOrReplace as it ignores concurrency checks
            batch.Add(update ? TableOperation.Replace(toPersist) : TableOperation.Insert(toPersist));
        }

        DictionaryTableEntity ToDictionaryTableEntity(object entity, DictionaryTableEntity toPersist, IEnumerable<PropertyInfo> properties)
        {
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType == typeof (byte[]))
                {
                    toPersist[propertyInfo.Name]= new EntityProperty((byte[]) propertyInfo.GetValue(entity, null)) ;
                }
                else if (propertyInfo.PropertyType == typeof (bool))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((bool)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (DateTime))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((DateTime)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (Guid))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Guid)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (Int32))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Int32)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (Int64))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Int64)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Double))
                {
                    toPersist[propertyInfo.Name] = new EntityProperty((Double)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (string))
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
    }
    
    //todo: refactor to feature, similar to this
    //public class AzureTablesSagaStorage : Feature
    //{
    //    public AzureTablesSagaStorage()
    //    {
    //        //Default(s => s.SetDefault('mywhatever', "something"));
    //        DependsOn<Sagas>();
    //    }

    //    protected override void Setup(FeatureConfigurationContext context)
    //    {
    //        var mywhatever = context.Settings.Get<string>("whatever");
    //    }
    //}

    //public class AzureTableStorage : PersistenceDefinition
    //{
    //}

    //internal class AzureTableStorageConfigurer: IConfigurePersistence<AzureTableStorage>
    //{
    //    public void Enable(Configure config)
    //    {
    //        config.Settings.EnableFeatureByDefault<AzureTablesSagaStorage>();
    //    }
    //}

    //public static class SagaSpecificSettings
    //{
    //    public static void SomeCoolSetting(this PersistenceConfiguration config, string mywhatever )
    //    {
    //        config.Config.Settings.Set("whatever", mywhatever);
    //    }
    //}

    //public class Program
    //{
    //    public static void Main()
    //    {
    //        Configure.With()
    //            .UsePersistence<AzureTableStorage>(t => t.SomeCoolSetting("true"));
    //    }
    //}
}
