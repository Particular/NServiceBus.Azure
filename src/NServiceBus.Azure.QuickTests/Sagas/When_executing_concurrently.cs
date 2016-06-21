namespace NServiceBus.AzureStoragePersistence.Tests
{
    using System;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Saga;
    using SagaPersisters.Azure;

    /// <summary>
    /// These tests try to mimic different concurrent scenarios using two persiters trying to access the same saga.
    /// </summary>
    public class When_executing_concurrently:BaseAzureSagaPersisterTest
    {
        public When_executing_concurrently()
        {
            cloudTable = this.sagaTable;
        }

        [SetUp]
        public void SetUp()
        {
            cloudTable.CreateIfNotExistsAsync();
            persister1 = new AzureSagaPersister(cloudStorageAccount, true);
            persister2 = new AzureSagaPersister(cloudStorageAccount, true);

            // clear whole table
            var entities = cloudTable.ExecuteQuery(new TableQuery<TableEntity>());
            foreach (var te in entities)
            {
                cloudTable.DeleteIgnoringNotFound(te);
            }
        }

        [Test(Description = "The test covering a scenario, when a secondary index wasn't deleted properly")]
        public void Should_not_find_saga_when_primary_is_removed_but_secondary_exists()
        {
            const string v = "1";
            Save(persister1, v, Id1);

            // get by property just to load to cache
            GetByCorrelationProperty(persister2);

            DeletePrimary(Id1);

            // only secondary exists now, ensure it's null
            var byProperty = GetByCorrelationProperty(persister2);
            Assert.IsNull(byProperty);
        }

        [Test(Description = "The test covering a scenario, when a secondary index wasn't deleted properly")]
        public void Should_enable_saving_another_saga_with_same_correlation_id_as_completed()
        {
            const string v = "1";
            Save(persister1, v, Id1);

            // get by property just to load to cache
            GetByCorrelationProperty(persister2);

            DeletePrimary(Id1);

            const string v2 = "2";

            // save a new saga with the same correlation id
            Save(persister1, v2, Id2);

            var saga = GetByCorrelationProperty(persister2);
            AssertSaga(saga, v2, Id2);
        }

        void DeletePrimary(Guid sagaId)
        {
            var entities = cloudTable.ExecuteQuery(new TableQuery<TableEntity>());
            Guid guid;
            var primary = entities.Single(te => Guid.TryParse(te.PartitionKey, out guid) && guid == sagaId);
            cloudTable.DeleteIgnoringNotFound(primary);
        }

        [Test]
        public void Should_enable_insert_saga_again_through_same_persister()
        {
            Should_enable_insert_saga_again(persister1);
        }

        [Test]
        public void Should_enable_insert_saga_again_through_another_persister()
        {
            Should_enable_insert_saga_again(persister2);
        }

        void Should_enable_insert_saga_again(ISagaPersister p)
        {
            const string v = "1";

            Save(persister1, v, Id1);

            var saga1 = Get(persister1, Id1);
            var saga2 = Get(persister2, Id1);
            var saga1ByProperty = GetByCorrelationProperty(persister1);
            var saga2ByProperty = GetByCorrelationProperty(persister2);

            AssertSaga(saga1, v, Id1);
            AssertSaga(saga2, v, Id1);
            AssertSaga(saga1ByProperty, v, Id1);
            AssertSaga(saga2ByProperty, v, Id1);

            Complete(saga1, persister1);

            saga1 = Get(persister1, Id1);
            saga2 = Get(persister2, Id1);
            saga1ByProperty = GetByCorrelationProperty(persister1);
            saga2ByProperty = GetByCorrelationProperty(persister2);

            Assert.IsNull(saga1);
            Assert.IsNull(saga2);
            Assert.IsNull(saga1ByProperty);
            Assert.IsNull(saga2ByProperty);

            const string v2 = "2";
            Save(p, v2, Id2);

            saga1 = Get(persister1, Id2);
            saga2 = Get(persister2, Id2);
            saga1ByProperty = GetByCorrelationProperty(persister1);
            saga2ByProperty = GetByCorrelationProperty(persister2);

            AssertSaga(saga1, v2, Id2);
            AssertSaga(saga2, v2, Id2);
            AssertSaga(saga1ByProperty, v2, Id2);
            AssertSaga(saga2ByProperty, v2, Id2);
        }

        static void Complete(IContainSagaData saga, ISagaPersister persister)
        {
            persister.Complete(saga);
        }

        static void AssertSaga(ConcurrentSagaData saga, string value, Guid id)
        {
            Assert.NotNull(saga);
            Assert.AreEqual(id, saga.Id);
            Assert.AreEqual(SagaCorrelationPropertyValue.Value, saga.CorrelationId);
            Assert.AreEqual(value, saga.Value);
        }

        static ConcurrentSagaData Get(ISagaPersister persister, Guid id)
        {
            return persister.Get<ConcurrentSagaData>(id);
        }

        static ConcurrentSagaData GetByCorrelationProperty(ISagaPersister persister)
        {
            return persister.Get<ConcurrentSagaData>(SagaCorrelationPropertyValue.Name, SagaCorrelationPropertyValue.Value);
        }

        static void Save(ISagaPersister persister, string value, Guid id)
        {
            persister.Save(new ConcurrentSagaData
            {
                Id = id,
                CorrelationId = CorrelationIdValue,
                Value = value
            });
        }

        readonly CloudTable cloudTable;

        AzureSagaPersister persister1;
        AzureSagaPersister persister2;
        const string CorrelationIdValue = "DB0F4000-5B9C-4ADE-9AB0-04305A5CABBD";

        static readonly Guid Id1 = new Guid("7FCF55F6-4AEB-40C7-86B9-2AB535664381");
        static readonly Guid Id2 = new Guid("2C739583-0077-4482-BA6E-E569DD129BD6");
        static readonly SagaCorrelationProperty SagaCorrelationPropertyValue = new SagaCorrelationProperty("CorrelationId", CorrelationIdValue);

        class ConcurrentSagaData : ContainSagaData
        {
            public string CorrelationId { get; set; }
            public string Value { get; set; }
        }
    }
}