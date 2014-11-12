namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues.AcceptanceTests.Sending
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_message_is_sent_with_time_to_be_received_set_to_more_than_7_days : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_exception()
        {
            var context = new Context();
            Scenario.Define(context)
                .WithEndpoint<ReceiverEndPoint>(b => b.Given((bus, c) =>
                {
                    var exception = Assert.Throws<InvalidOperationException>(() => bus.SendLocal(new MessageNotToBeSent()));
                    var expectedMessage = string.Format("TimeToBeReceived is set to more than 7 days (maximum for Azure Storage queue) for message type '{0}'.", typeof(MessageNotToBeSent).FullName);
                    Assert.AreEqual(expectedMessage, exception.Message);
                    c.ExceptionReceived = true;
                }))
                .Done(c => c.ExceptionReceived)
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
        }

        public class ReceiverEndPoint : EndpointConfigurationBuilder
        {
            public Context Context { get; set; }

            public ReceiverEndPoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        [TimeToBeReceived("7.00:00:01")]
        public class MessageNotToBeSent : IMessage
        {
        }
    }
}
