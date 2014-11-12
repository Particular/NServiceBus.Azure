namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues.AcceptanceTests.Sending
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_message_is_sent_with_time_to_be_received_set_to_zero : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_send()
        {
            var context = new Context();
            Scenario.Define(context)
                .WithEndpoint<ReceiverEndPoint>(b => b.Given((bus, c) => bus.SendLocal(new MessageNotToBeSent())))
                .Run();
            Assert.IsFalse(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class ReceiverEndPoint : EndpointConfigurationBuilder
        {
            public ReceiverEndPoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MessageNotToBeSent>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MessageNotToBeSent message)
                {
                    Context.WasCalled = true;
                }
            }

        }

        [TimeToBeReceived("00:00:00")]
        public class MessageNotToBeSent : IMessage
        {
        }
    }
}
