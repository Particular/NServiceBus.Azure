namespace NServiceBus.AzureStoragePersistence.Tests
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;
    using NUnit.Framework;
    using Saga;

    public class When_saga_is_completed : BaseAzureSagaPersisterTest
    {
        readonly CloudTable cloudTable;

        public When_saga_is_completed()
        {
            cloudTable = tables.GetTableReference(typeof(RemovingSecondaryIndexState).Name);
        }

        [SetUp]
        public void SetUp()
        {
            // clear whole table
            var entities = cloudTable.ExecuteQuery(new TableQuery<TableEntity>());
            foreach (var te in entities)
            {
                cloudTable.DeleteIgnoringNotFound(te);
            }
        }

        [Test]
        public void Entities_should_be_removed()
        {
            const string orderID = "unique-order-id";
            var state = new RemovingSecondaryIndexState
            {
                OrderId = orderID,
                Id = Guid.NewGuid()
            };

            persister.Save(state);

            // if not retrieved by the secondary index, it fails
            // state = persister.Get<RemovingSecondaryIndexState>("OrderId", orderID);

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