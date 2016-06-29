namespace NServiceBus.AzureStoragePersistence.Tests
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;
    using NUnit.Framework;
    using Saga;

    public class When_saga_is_completed : BaseAzureSagaPersisterTest
    {
        const int GetById = 1;
        const int GetByProperty = 2;

        readonly CloudTable cloudTable;

        public When_saga_is_completed()
        {
            cloudTable = tables.GetTableReference(typeof(RemovingSecondaryIndexState).Name);
            cloudTable.CreateIfNotExists();
        }

        [SetUp]
        public void SetUp()
        {
            // clear whole table
            var entities = cloudTable.ExecuteQuery(new TableQuery<TableEntity>());
            foreach (var te in entities)
            {
                te.ETag = "*";
                cloudTable.DeleteIgnoringNotFound(te);
            }
        }

        [TestCase(GetById)]
        [TestCase(GetByProperty)]
        public void Entities_should_be_removed(int getBy)
        {
            const string orderID = "unique-order-id";
            var id = Guid.NewGuid();

            var state = new RemovingSecondaryIndexState
            {
                OrderId = orderID,
                Id = id
            };

            persister.Save(state);

            // get saga
            switch (getBy)
            {
                case GetById:
                    state = persister.Get<RemovingSecondaryIndexState>(id);
                    break;
                case GetByProperty:
                    state = persister.Get<RemovingSecondaryIndexState>("OrderId", orderID);
                    break;
            }

            persister.Complete(state);

            var count = cloudTable.CountAllEntities();
            Assert.AreEqual(0, count);
        }

        private sealed class RemovingSecondaryIndexState : IContainSagaData
        {
            [Unique]
            public string OrderId { get; set; }

            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
        }
    }
}