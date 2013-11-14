namespace NServiceBus.Azure.QuickTests
{
    using System;
    using NUnit.Framework;
    using Transports.WindowsAzureServiceBus;

    [TestFixture]
    [Category("Azure")]
    public class When_determining_subscription_names
    {
        [Test]
        public void Should_not_exceed_fifthy_characters_and_replace_by_a_deterministic_guid()
        {
            var subscriptionname = AzureServiceBusSubscriptionNamingConvention.Apply(typeof(SomeEventWithAnInsanelyLongName));

            Guid guid;
            Assert.IsTrue(Guid.TryParse(subscriptionname, out guid));
        }
    }

    public class SomeEventWithAnInsanelyLongName : IEvent
    {
    }
}