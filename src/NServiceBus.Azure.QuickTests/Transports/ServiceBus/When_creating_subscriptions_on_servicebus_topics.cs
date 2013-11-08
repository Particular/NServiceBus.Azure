namespace NServiceBus.Azure.QuickTests
{
    using NUnit.Framework;
    using Transports.WindowsAzureServiceBus;

    [TestFixture]
    [Category("Azure")]
    public class When_creating_subscriptions_on_servicebus_topics
    {
        [Test]
        public void Should_filter_on_subscribed_eventtype_somewhere_in_enclosed_messagetypes_header()
        {
            var eventType = typeof(SomeEvent);

            var filter = new ServicebusSubscriptionFilterBuilder().BuildFor(eventType);

            Assert.AreEqual(filter, "[NServiceBus.EnclosedMessageTypes] LIKE 'NServiceBus.Azure.QuickTests.SomeEvent%' OR [NServiceBus.EnclosedMessageTypes] LIKE '%NServiceBus.Azure.QuickTests.SomeEvent%' OR [NServiceBus.EnclosedMessageTypes] LIKE '%NServiceBus.Azure.QuickTests.SomeEvent' OR [NServiceBus.EnclosedMessageTypes] = 'NServiceBus.Azure.QuickTests.SomeEvent'");
        }
    }

    public class SomeEvent : IEvent
    {
    }
}