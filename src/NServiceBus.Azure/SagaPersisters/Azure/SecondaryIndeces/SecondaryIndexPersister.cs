namespace NServiceBus.SagaPersisters.Azure.SecondaryIndeces
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq.Expressions;
    using System.Net;
    using System.Reflection;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using NServiceBus.Sagas;
    using Saga;

    public class SecondaryIndexPersister
    {
        public delegate Guid[] ScanForSaga(Type sagaType, string propertyName, object propertyValue);

        const int LRUCapacity = 1000;
        readonly LRUCache<PartitionRowKeyTuple, Guid> cache = new LRUCache<PartitionRowKeyTuple, Guid>(LRUCapacity);
        readonly Func<Type, CloudTable> getTableForSaga;
        readonly Action<IContainSagaData> persist;
        readonly ScanForSaga scanner;

        public SecondaryIndexPersister(Func<Type, CloudTable> getTableForSaga, ScanForSaga scanner, Action<IContainSagaData> persist)
        {
            this.getTableForSaga = getTableForSaga;
            this.scanner = scanner;
            this.persist = persist;
        }

        public PartitionRowKeyTuple Insert(IContainSagaData sagaData)
        {
            var sagaType = sagaData.GetType();
            var table = getTableForSaga(sagaType);
            var ix = IndexDefintion.Get(sagaType);
            if (ix == null)
            {
                return null;
            }

            var propertyValue = ix.Accessor(sagaData);
            var key = ix.BuildTableKey(propertyValue);

            var entity = new SecondaryIndexTableEntity
            {
                SagaId = sagaData.Id,
                InitialSagaData = SagaDataSerializer.SerializeSagaData(sagaData),
                PartitionKey = key.PartitionKey,
                RowKey = key.RowKey
            };

            // the insert plan is following:
            // 1) try insert the 2nd index row
            // 2) if it fails, another worker has done it
            // 3) ensure that the primary is stored, throwing an exception afterwards in any way

            try
            {
                table.Execute(TableOperation.Insert(entity));
                return key;
            }
            catch (StorageException ex)
            {
                var indexRowAlreadyExists = IsConflict(ex);
                if (indexRowAlreadyExists)
                {
                    var existingSecondaryIndexEntity = table.Execute(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, key.RowKey)).Result as SecondaryIndexTableEntity;
                    var data = existingSecondaryIndexEntity?.InitialSagaData;
                    if (data != null)
                    {
                        var deserializeSagaData = SagaDataSerializer.DeserializeSagaData(sagaType, data);

                        // saga hasn't been saved under primary key. Try to store it
                        try
                        {
                            persist(deserializeSagaData);
                            return key;
                        }
                        catch (StorageException e)
                        {
                            if (IsConflict(e))
                            {
                                // swallow ex as another worker created the primary under this key
                            }
                        }
                        throw new RetryNeededException();
                    }
                    // ReSharper disable once RedundantIfElseBlock to make it visible for a reader
                    else
                    {
                        // data is null, this means that either the entry has been created as the secondary index after scanning the table or after storing the primary
                        var sagaId = existingSecondaryIndexEntity?.SagaId;
                        if (sagaId != null)
                        {
                            var primary = table.Execute(TableOperation.Retrieve<TableEntity>(sagaId.Value.ToString(), string.Empty)).Result;
                            if (primary != null)
                            {
                                // if the primary exist though, it means that a retry is required as the previous saga with the specified correlation hasn't been completed yet
                                // and the secondary index isn't a leftover from a completion
                                throw new RetryNeededException();
                            }
                        }

                        try
                        {
                            table.Execute(TableOperation.Delete(existingSecondaryIndexEntity));
                            table.Execute(TableOperation.Insert(entity));
                            return key;
                        }
                        catch (Exception exception)
                        {
                            throw new RetryNeededException(exception);
                        }
                    }

                }

                throw;
            }
        }

        public Guid? FindSagaIdAndCreateIndexEntryIfNotFound<TSagaData>(string propertyName, object propertyValue)
            where TSagaData : IContainSagaData
        {
            var sagaType = typeof(TSagaData);
            var key = TryBuildKey(propertyName, propertyValue, sagaType);

            if (key == null)
            {
                return null;
            }

            Guid guid;
            if (cache.TryGet(key, out guid))
            {
                return guid;
            }

            var table = getTableForSaga(sagaType);
            var exec = table.Execute(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, key.RowKey));
            var secondaryIndexEntry = exec.Result as SecondaryIndexTableEntity;
            if (secondaryIndexEntry != null)
            {
                cache.Put(key, secondaryIndexEntry.SagaId);
                return secondaryIndexEntry.SagaId;
            }

            var ids = scanner(sagaType, propertyName, propertyValue);
            if (ids == null || ids.Length == 0)
            {
                return null;
            }

            if (ids.Length > 1)
            {
                throw new DuplicatedSagaFoundException(sagaType, propertyName, ids);
            }

            var id = ids[0];

            var entity = CreateIndexingOnlyEntity(key, id);

            try
            {
                table.Execute(TableOperation.Insert(entity));
            }
            catch (StorageException)
            {
                throw new RetryNeededException();
            }

            cache.Put(key, id);
            return id;
        }

        /// <summary>
        /// Creates an indexing only entity, without payload of the primary.
        /// </summary>
        static SecondaryIndexTableEntity CreateIndexingOnlyEntity(PartitionRowKeyTuple key, Guid id)
        {
            var entity = new SecondaryIndexTableEntity();
            key.Apply(entity);
            entity.SagaId = id;
            return entity;
        }

        /// <summary>
        /// Invalidates the secondary index cache if any exists for the specified property value.
        /// </summary>
        public void InvalidateCacheIfAny(string propertyName, object propertyValue, Type sagaType)
        {
            var key = TryBuildKey(propertyName, propertyValue, sagaType);
            if (key != null)
            {
                cache.Remove(key);
            }
        }

        static PartitionRowKeyTuple TryBuildKey(string propertyName, object propertyValue, Type sagaType)
        {
            if (string.IsNullOrEmpty(propertyName) || propertyValue == null)
            {
                return null;
            }
            return IndexDefintion.Get(sagaType).BuildTableKey(propertyValue);
        }

        public Guid? FindPossiblyCreatingIndexEntry<TSagaData>(string propertyName, object propertyValue)
            where TSagaData : IContainSagaData
        {
            var sagaType = typeof(TSagaData);
            var ix = IndexDefintion.Get(sagaType);
            if (ix == null)
            {
                throw new ArgumentException($"Saga '{typeof(TSagaData)}' has no correlation properties. Ensure that your saga is correlated by this property and only then, mark it with `Unique` attribute.");
            }

            ix.ValidateProperty(propertyName);

            var key = ix.BuildTableKey(propertyValue);

            Guid guid;
            if (cache.TryGet(key, out guid))
            {
                return guid;
            }

            var table = getTableForSaga(sagaType);
            var secondaryIndexEntry = table.Execute(TableOperation.Retrieve<SecondaryIndexTableEntity>(key.PartitionKey, key.RowKey)).Result as SecondaryIndexTableEntity;
            if (secondaryIndexEntry != null)
            {
                cache.Put(key, secondaryIndexEntry.SagaId);
                return secondaryIndexEntry.SagaId;
            }

            var ids = scanner(sagaType, propertyName, propertyValue);
            if (ids == null || ids.Length == 0)
            {
                return null;
            }

            if (ids.Length > 1)
            {
                throw new DuplicatedSagaFoundException(sagaType, propertyName, ids);
            }

            var id = ids[0];

            var entity = CreateIndexingOnlyEntity(key, id);

            try
            {
                table.Execute(TableOperation.Insert(entity));
            }
            catch (StorageException)
            {
                throw new RetryNeededException();
            }

            cache.Put(key, id);
            return id;
        }

        public void RemoveSecondary(Type sagaType, PartitionRowKeyTuple secondaryIndexKey)
        {
            var table = getTableForSaga(sagaType);
            var e = new TableEntity
            {
                ETag = "*"
            };

            secondaryIndexKey.Apply(e);
            cache.Remove(secondaryIndexKey);
            table.DeleteIgnoringNotFound(e);
        }

        public void MarkAsHavingPrimaryPersisted(IContainSagaData sagaData)
        {
            var sagaType = sagaData.GetType();
            var table = getTableForSaga(sagaType);
            var ix = IndexDefintion.Get(sagaType);
            if (ix == null)
            {
                return;
            }

            var propertyValue = ix.Accessor(sagaData);
            var secondaryIndexKey = ix.BuildTableKey(propertyValue);

            var secondaryIndexTableEntity = new SecondaryIndexTableEntity
                {
                    InitialSagaData = null, // this nullification is required for v4.3 of WindowsAzure.Storage. Changed to not be needed in WindowsAzure.Storage v7
                    SagaId = sagaData.Id
                };
            secondaryIndexKey.Apply(secondaryIndexTableEntity);

            secondaryIndexTableEntity.ETag = "*";

            table.ExecuteAsync(TableOperation.Replace(secondaryIndexTableEntity));
        }

        private static bool IsConflict(StorageException ex)
        {
            return ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.Conflict;
        }

        private class IndexDefintion
        {
            static readonly ConcurrentDictionary<Type, object> sagaToIndex = new ConcurrentDictionary<Type, object>();
            static readonly object NullValue = new object();

            static readonly ParameterExpression ObjectParameter = Expression.Parameter(typeof(object));
            readonly string propertyName;

            readonly string sagaTypeName;

            private IndexDefintion(Type sagaType, PropertyInfo pi)
            {
                sagaTypeName = sagaType.FullName;
                propertyName = pi.Name;
                Accessor = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(ObjectParameter, sagaType), pi), typeof(object)), ObjectParameter).Compile();
            }

            public Func<object, object> Accessor { get; }

            public static IndexDefintion Get(Type sagaType)
            {
                var index = sagaToIndex.GetOrAdd(sagaType, type =>
                {
                    var pi = UniqueAttribute.GetUniqueProperty(sagaType);
                    if (pi == null)
                    {
                        return NullValue;
                    }

                    return new IndexDefintion(sagaType, pi);
                });

                if (ReferenceEquals(index, NullValue))
                {
                    return null;
                }

                return (IndexDefintion) index;
            }

            public void ValidateProperty(string propertyName)
            {
                if (this.propertyName != propertyName)
                {
                    throw new ArgumentException($"The following saga '{sagaTypeName}' is not indexed by '{propertyName}'. The only secondary index is defined for '{this.propertyName}'. " +
                                                $"Ensure that the saga is correlated properly.");
                }
            }

            public PartitionRowKeyTuple BuildTableKey(object propertyValue)
            {
                return new PartitionRowKeyTuple($"Index_{sagaTypeName}_{propertyName}_{Serialize(propertyValue)}", "");
            }

            private static string Serialize(object propertyValue)
            {
                using (var sw = new StringWriter())
                {
                    new JsonSerializer().Serialize(sw, propertyValue);
                    sw.Flush();
                    return sw.ToString();
                }
            }
        }
    }
}