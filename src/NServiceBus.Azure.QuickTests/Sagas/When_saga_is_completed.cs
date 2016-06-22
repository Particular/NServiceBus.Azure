//using System;
//using System.Threading.Tasks;
//using Microsoft.WindowsAzure.Storage;
//using NServiceBus;
//using NServiceBus.AcceptanceTesting;
//using NServiceBus.AcceptanceTests;
//using NServiceBus.AcceptanceTests.EndpointTemplates;
//using NServiceBus.AzureStoragePersistence.Tests;
//using NServiceBus.Persistence.AzureStorage.AcceptanceTests;
//using NServiceBus.Saga;
//using NUnit.Framework;
//
//public class When_saga_is_completed : NServiceBusAcceptanceTest
//{
//    [Test]
//    public async Task Entities_should_be_removed()
//    {
//        var account = CloudStorageAccount.Parse(ConfigureEndpointAzureStoragePersistence.GetConnectionString());
//        var name = typeof(StartComplete.RemovingSecondaryIndexState).Name;
//        var table = account.CreateCloudTableClient().GetTableReference(name);
//        await table.CreateIfNotExistsAsync().ConfigureAwait(false);
//        await table.DeleteAllEntities();
//
//        var guid = Guid.NewGuid().ToString();
//
//        await Scenario.Define<Context>()
//            .WithEndpoint<ReceiverWithSagas>(b => b.When(async session =>
//            {
//                await session.SendLocal(new Start
//                {
//                    OrderId = guid
//                });
//                await session.SendLocal(new Complete
//                {
//                    OrderId = guid
//                });
//            }))
//            .Done(c => c.Completed)
//            .Run().ConfigureAwait(false);
//
//        var count = await table.CountAllEntities();
//        Assert.AreEqual(0, count);
//    }
//
//    public class Context : ScenarioContext
//    {
//        public bool Completed { get; set; }
//    }
//
//    public class ReceiverWithSagas : EndpointConfigurationBuilder
//    {
//        public ReceiverWithSagas()
//        {
//            EndpointSetup<DefaultServer>(cfg => cfg.LimitMessageProcessingConcurrencyTo(1));
//        }
//    }
//
//    public class StartComplete : Saga<StartComplete.RemovingSecondaryIndexState>,
//        IAmStartedByMessages<Start>,
//        IHandleMessages<Complete>
//    {
//        public Context Context { get; set; }
//
//        public Task Handle(Start message, IMessageHandlerContext context)
//        {
//            Data.OrderId = message.OrderId;
//            return Task.FromResult(0);
//        }
//
//        public Task Handle(Complete message, IMessageHandlerContext context)
//        {
//            MarkAsComplete();
//            Context.Completed = true;
//            return Task.FromResult(0);
//        }
//
//        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RemovingSecondaryIndexState> mapper)
//        {
//            mapper.ConfigureMapping<Start>(m => m.OrderId)
//                .ToSaga(s => s.OrderId);
//            mapper.ConfigureMapping<Complete>(m => m.OrderId)
//                .ToSaga(s => s.OrderId);
//        }
//
//        public class RemovingSecondaryIndexState : IContainSagaData
//        {
//            public virtual string OrderId { get; set; }
//            public virtual Guid Id { get; set; }
//            public virtual string Originator { get; set; }
//            public virtual string OriginalMessageId { get; set; }
//        }
//    }
//
//    public class Start : ICommand
//    {
//        public string OrderId { get; set; }
//    }
//
//    public class Complete : ICommand
//    {
//        public string OrderId { get; set; }
//    }
//}