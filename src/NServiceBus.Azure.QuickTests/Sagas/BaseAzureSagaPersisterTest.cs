namespace NServiceBus.AzureStoragePersistence.Tests
{
    using System;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Saga;
    using SagaPersisters.Azure;

    public abstract class BaseAzureSagaPersisterTest
    {
        protected readonly CloudStorageAccount cloudStorageAccount;
        protected readonly string connectionString;
        protected readonly ISagaPersister persister;
        protected readonly CloudTable sagaTable;
        protected readonly CloudTableClient tables;

        protected BaseAzureSagaPersisterTest()
        {
            connectionString = Environment.GetEnvironmentVariable("AzureStoragePersistence.ConnectionString");
            cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            persister = new AzureSagaPersister(cloudStorageAccount, true);
            tables = cloudStorageAccount.CreateCloudTableClient();
            sagaTable = tables.GetTableReference(typeof(TwoInstanceSagaState).Name);
        }

        protected void Clear()
        {
            foreach (var dte in sagaTable.ExecuteQuery(new TableQuery()))
            {
                sagaTable.Execute(TableOperation.Delete(dte));
            }
        }

        protected void Insert(ITableEntity entity)
        {
            sagaTable.Execute(TableOperation.Insert(entity));
        }

        protected static TwoInstanceSagaStateEntity BuildState(string orderID, Guid id)
        {
            return new TwoInstanceSagaStateEntity
            {
                OrderId = orderID,
                Id = id,
                PartitionKey = id.ToString(),
                RowKey = id.ToString()
            };
        }

        public class TwoInstanceSagaState : IContainSagaData
        {
            [Unique]
            public virtual string OrderId { get; set; }

            public virtual Guid Id { get; set; }
            public virtual string Originator { get; set; }
            public virtual string OriginalMessageId { get; set; }
        }

        public class TwoInstanceSagaStateEntity : TableEntity
        {
            public virtual string OrderId { get; set; }
            public virtual Guid Id { get; set; }
            public virtual string Originator { get; set; }
            public virtual string OriginalMessageId { get; set; }

            public TwoInstanceSagaState ToState()
            {
                return new TwoInstanceSagaState
                {
                    Id = Id,
                    OrderId = OrderId,
                    OriginalMessageId = OriginalMessageId,
                    Originator = Originator
                };
            }
        }
    }
}