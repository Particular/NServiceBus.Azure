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
            GetSagaDataByCorrelationProperty(persister2);

            DeleteSagaRecord(Id1);

            // only secondary exists now, ensure it's null
            var sagaData = GetSagaDataByCorrelationProperty(persister2);
            Assert.IsNull(sagaData);
        }

        [Test(Description = "The test covering a scenario, when a secondary index wasn't deleted properly")]
        public void Should_enable_saving_another_saga_with_same_correlation_id_as_completed()
        {
            const string v = "1";
            Save(persister1, v, Id1);

            // get by property just to load to cache
            GetSagaDataByCorrelationProperty(persister2);

            DeleteSagaRecord(Id1);

            const string v2 = "2";

            // save a new saga with the same correlation id
            Save(persister1, v2, Id2);

            var saga = GetSagaDataByCorrelationProperty(persister2);
            AssertSaga(saga, v2, Id2);
        }

        void DeleteSagaRecord(Guid sagaId)
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
            const string version1 = "1";

            Save(persister1, version1, Id1);

            var sagaData1 = Get(persister1, Id1);
            var sagaData2 = Get(persister2, Id1);
            var sagaData1ByProperty = GetSagaDataByCorrelationProperty(persister1);
            var sagaData2ByProperty = GetSagaDataByCorrelationProperty(persister2);

            AssertSaga(sagaData1, version1, Id1);
            AssertSaga(sagaData2, version1, Id1);
            AssertSaga(sagaData1ByProperty, version1, Id1);
            AssertSaga(sagaData2ByProperty, version1, Id1);

            Complete(sagaData1, persister1);

            sagaData1 = Get(persister1, Id1);
            sagaData2 = Get(persister2, Id1);
            sagaData1ByProperty = GetSagaDataByCorrelationProperty(persister1);
            sagaData2ByProperty = GetSagaDataByCorrelationProperty(persister2);

            Assert.IsNull(sagaData1);
            Assert.IsNull(sagaData2);
            Assert.IsNull(sagaData1ByProperty);
            Assert.IsNull(sagaData2ByProperty);

            const string version2 = "2";
            Save(p, version2, Id2);

            sagaData1 = Get(persister1, Id2);
            sagaData2 = Get(persister2, Id2);
            sagaData1ByProperty = GetSagaDataByCorrelationProperty(persister1);
            sagaData2ByProperty = GetSagaDataByCorrelationProperty(persister2);

            AssertSaga(sagaData1, version2, Id2);
            AssertSaga(sagaData2, version2, Id2);
            AssertSaga(sagaData1ByProperty, version2, Id2);
            AssertSaga(sagaData2ByProperty, version2, Id2);
        }

        static void Complete(IContainSagaData sagaData, ISagaPersister persister)
        {
            persister.Complete(sagaData);
        }

        static void AssertSaga(TwoInstanceSagaState sagaData, string value, Guid id)
        {
            Assert.NotNull(sagaData);
            Assert.AreEqual(id, sagaData.Id);
            Assert.AreEqual(SagaCorrelationPropertyValue.Value, sagaData.OrderId);
            Assert.AreEqual(value, sagaData.OriginalMessageId);
        }

        static TwoInstanceSagaState Get(ISagaPersister persister, Guid id)
        {
            return persister.Get<TwoInstanceSagaState>(id);
        }

        static TwoInstanceSagaState GetSagaDataByCorrelationProperty(ISagaPersister persister)
        {
            return persister.Get<TwoInstanceSagaState>(SagaCorrelationPropertyValue.Name, SagaCorrelationPropertyValue.Value);
        }

        static void Save(ISagaPersister persister, string value, Guid id)
        {
            persister.Save(new TwoInstanceSagaState
            {
                Id = id,
                OrderId =  CorrelationIdValue,
                OriginalMessageId = value
            });
        }

        readonly CloudTable cloudTable;

        AzureSagaPersister persister1;
        AzureSagaPersister persister2;
        const string CorrelationIdValue = "DB0F4000-5B9C-4ADE-9AB0-04305A5CABBD";

        static readonly Guid Id1 = new Guid("7FCF55F6-4AEB-40C7-86B9-2AB535664381");
        static readonly Guid Id2 = new Guid("2C739583-0077-4482-BA6E-E569DD129BD6");
        static readonly SagaCorrelationProperty SagaCorrelationPropertyValue = new SagaCorrelationProperty(nameof(TwoInstanceSagaState.OrderId), CorrelationIdValue);
    }
}