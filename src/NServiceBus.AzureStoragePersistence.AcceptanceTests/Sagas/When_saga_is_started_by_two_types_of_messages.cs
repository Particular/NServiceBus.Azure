namespace NServiceBus.AzureStoragePersistence.AcceptanceTests.Sagas
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Saga;
    using NServiceBus.SagaPersisters.Azure;
    using NUnit.Framework;

    public class When_saga_is_started_by_two_types_of_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public void Every_saga_should_be_started_once_and_updated_by_second_message()
        {
            const int expectedNumberOfCreatedSagas = 20;

            var guids = new HashSet<string>(Enumerable.Repeat(1, expectedNumberOfCreatedSagas).Select(i => Guid.NewGuid().ToString())).OrderBy(s => s);

            var context = new Context();
            Scenario.Define(context)
                    .WithEndpoint<ReceiverWithSagas>(b => b.Given((bus, c) =>
                    {
                        foreach (var guid in guids)
                        {
                            bus.SendLocal(new OrderBilled
                            {
                                OrderId = guid
                            });
                            bus.SendLocal(new OrderPlaced
                            {
                                OrderId = guid
                            });
                        }
                    }))
                    .AllowExceptions()
                    .Done(c => c.CompletedIds.OrderBy(s => s).ToArray().Intersect(guids).Count() == expectedNumberOfCreatedSagas)
                    .Run();

            CollectionAssert.AreEquivalent(guids, context.CompletedIds.OrderBy(s => s).ToArray());

            var retries = AllIndexesOf(context.Exceptions, typeof(RetryNeededException).FullName).Count();
            Console.WriteLine($"Sagas with retries/Total no of sagas {retries}/{expectedNumberOfCreatedSagas}");
        }

        private static IEnumerable<int> AllIndexesOf(string str, string value)
        {
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index, StringComparison.Ordinal);
                if (index == -1)
                    yield break;
                yield return index;
            }
        }

        public class ProvideConfiguration : IProvideConfiguration<TransportConfig>
        {
            public TransportConfig GetConfiguration()
            {
                return new TransportConfig
                {
                    MaximumConcurrencyLevel = 3,
                };
            }
        }

        public class Context : ScenarioContext
        {
            private readonly ConcurrentDictionary<string, string> completed = new ConcurrentDictionary<string, string>();
            public int CompletedIdsCount => completed.Count;
            public IEnumerable<string> CompletedIds => completed.Keys;

            public void MarkAsCompleted(string orderId)
            {
                completed.AddOrUpdate(orderId, orderId, (o1, o2) => o1);
            }
        }

        public class ReceiverWithSagas : EndpointConfigurationBuilder
        {
            public ReceiverWithSagas()
            {
                EndpointSetup<DefaultServer>(
                    config =>
                    { });
            }
        }

        public class ShippingPolicy : Saga<ShippingPolicy.State>,
            IAmStartedByMessages<OrderPlaced>,
            IAmStartedByMessages<OrderBilled>
        {
            public Context Context { get; set; }

            public void Handle(OrderBilled message)
            {
                Data.OrderId = message.OrderId;
                Data.Billed = true;

                TryComplete();
            }

            public void Handle(OrderPlaced message)
            {
                Data.OrderId = message.OrderId;
                Data.Placed = true;

                TryComplete();
            }

            private void TryComplete()
            {
                if (Data.Billed && Data.Placed)
                {
                    MarkAsComplete();
                    Context.MarkAsCompleted(Data.OrderId);
                }
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<State> mapper)
            {
                mapper.ConfigureMapping<OrderPlaced>(m => m.OrderId)
                    .ToSaga(s => s.OrderId);
                mapper.ConfigureMapping<OrderBilled>(m => m.OrderId)
                    .ToSaga(s => s.OrderId);
            }

            public class State : IContainSagaData
            {
                [Unique]
                public virtual string OrderId { get; set; }
                public virtual bool Placed { get; set; }
                public virtual bool Billed { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }
        }

        public class OrderBilled : ICommand
        {
            public string OrderId { get; set; }
        }

        public class OrderPlaced : ICommand
        {
            public string OrderId { get; set; }
        }
    }
}