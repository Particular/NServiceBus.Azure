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
        const string QueueName1 = "address://test-queue";
        const string QueueName2 = "address://test-queue2";
        const string MachineName = "machineName";

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

            persister.Subscribe(new Address(QueueName1, MachineName), messageTypes);

            var subscribers = persister.GetSubscriberAddressesForMessage(messageTypes);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.Queue, Is.EqualTo(QueueName1));
            Assert.That(subscription.Machine, Is.EqualTo(MachineName));
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

            persister.Subscribe(new Address(QueueName1, MachineName), messageTypes);

            var subscribers = persister.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType(typeof(TestMessage))
            });

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.Queue, Is.EqualTo(QueueName1));
            Assert.That(subscription.Machine, Is.EqualTo(MachineName));
        }

        [Test]
        public void ensure_that_the_subscription_selects_proper_message_types()
        {
            var persister = SubscriptionTestHelper.CreateAzureSubscriptionStorage();

            persister.Subscribe(new Address(QueueName1, MachineName), new[]
            {
                new MessageType(typeof(TestMessage))
            });

            persister.Subscribe(new Address(QueueName2, MachineName), new[]
            {
                new MessageType(typeof(TestMessagea))
            });

            var subscribers = persister.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType(typeof(TestMessage))
            });

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.Queue, Is.EqualTo(QueueName1));
            Assert.That(subscription.Machine, Is.EqualTo(MachineName));
        }
    }

    class TestMessage
    {
    }

    class TestMessagea
    {
    }
}