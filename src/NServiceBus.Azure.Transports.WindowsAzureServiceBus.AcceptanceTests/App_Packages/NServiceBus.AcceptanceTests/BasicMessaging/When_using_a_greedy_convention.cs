﻿namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_a_greedy_convention : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<EndPoint>(b => b.Given((bus, context) => bus.SendLocal(new MyMessage
                    {Id = context.Id})))
                    .Done(c => c.WasCalled)
                    .Repeat(r =>r
                        .For(Transports.Msmq)
                    )
                    .Should(c => Assert.True(c.WasCalled, "The message handler should be called"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }

            public Guid Id { get; set; }
        }

        public class EndPoint : EndpointConfigurationBuilder
        {
            public EndPoint()
            {
                EndpointSetup<DefaultServer>(c => c.DefiningMessagesAs(MessageConvention));
            }

            static bool MessageConvention(Type t)
            {
                return t.Namespace != null && 
                    (t.Namespace.EndsWith(".Messages") ||  (t == typeof(MyMessage)));
            }
        }

        [Serializable]
        public class MyMessage
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }
            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.WasCalled = true;
            }
        }
    }
}
