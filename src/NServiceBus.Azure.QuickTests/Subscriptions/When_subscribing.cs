namespace NServiceBus.AzureStoragePersistence.Tests
{
    using System;
    using System.Linq;
    using Unicast.Subscriptions;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_subscribing : BaseAzureSagaPersisterTest
    {
        const string Q = "address://test-queue";
        const string M = "machineName";

        [SetUp]
        public void Setup()
        {
            SubscriptionTestHelper.PerformStorageCleanup();
        }

        [Test]
        public void ensure_that_the_subscription_is_persisted()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();
            var messageType = new MessageType(typeof(TestMessage));
            var messageTypes = new[]
            {
                messageType
            };

            persister.Subscribe(new Address(Q, M), messageTypes);

            var subscribers = persister.GetSubscriberAddressesForMessage(messageTypes);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.Queue, Is.EqualTo(Q));
            Assert.That(subscription.Machine, Is.EqualTo(M));
        }

        [Test]
        public void ensure_that_the_subscription_is_version_ignorant()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();

            var name = typeof(TestMessage).FullName;

            var messageTypes = new[]
            {
                new MessageType(name, new Version(1, 2, 3)),
                new MessageType(name, new Version(4, 2, 3)),
            };

            persister.Subscribe(new Address(Q, M), messageTypes);

            var subscribers = persister.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType(typeof(TestMessage))
            });

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.Queue, Is.EqualTo(Q));
            Assert.That(subscription.Machine, Is.EqualTo(M));
        }
    }

    class TestMessage
    {
    }
}