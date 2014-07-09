namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.QueueAndTopicByEndpoint
{
    using System;
    using Microsoft.ServiceBus.Messaging;
    using Settings;
    using Transports;

    /// <summary>
    /// Sends occur through queues, one for each endpoint, 
    /// publishes through a topic per endpoint, 
    /// receives on both it's own queue &amp; subscriptions per datatype
    /// </summary>

    internal class QueueAndTopicByEndpointTopology : ITopology
    {
        readonly Configure config;
        readonly ICreateMessagingFactories messagingFactories;
        readonly ICreateSubscriptions subscriptionCreator;
        readonly ICreateQueues queueCreator;
        readonly ICreateTopics topicCreator;
        readonly ICreateQueueClients queueClients; 
        readonly ICreateSubscriptionClients subscriptionClients;
        readonly ICreateTopicClients topicClients;

        internal QueueAndTopicByEndpointTopology(
            Configure config, 
            ICreateMessagingFactories messagingFactories,
            ICreateSubscriptions subscriptionCreator, 
            ICreateQueues queueCreator,
            ICreateTopics topicCreator, 
            ICreateQueueClients queueClients, 
            ICreateSubscriptionClients subscriptionClients,
            ICreateTopicClients topicClients)
        {
            this.config = config;
            this.messagingFactories = messagingFactories;
            this.subscriptionCreator = subscriptionCreator;
            this.queueCreator = queueCreator;
            this.topicCreator = topicCreator;
            this.queueClients = queueClients;
            this.subscriptionClients = subscriptionClients;
            this.topicClients = topicClients;
        }

        public void Initialize(ReadOnlySettings settings)
        {
            var queuename = NamingConventions.QueueNamingConvention(config, null, settings.EndpointName());
            Address.InitializeLocalAddress(queuename);
        }

        public INotifyReceivedBrokeredMessages Subscribe(Type eventType, Address address)
        {
            var publisherAddress = NamingConventions.PublisherAddressConventionForSubscriptions(config, address);
            var notifier = config.Builder.Build<AzureServiceBusSubscriptionNotifier>();
            notifier.SubscriptionClient = CreateSubscriptionClient(eventType, publisherAddress);
            //todo: notifier.BatchSize = maximumConcurrencyLevel;
            notifier.MessageType = eventType;
            notifier.Address = publisherAddress;
            return notifier;
        }

        private SubscriptionClient CreateSubscriptionClient(Type eventType, Address address)
        {
            var subscriptionname = NamingConventions.SubscriptionNamingConvention(config, eventType, config.Settings.EndpointName());
            var factory = messagingFactories.Create(address);

            try
            {
                var description = subscriptionCreator.Create(address, eventType, subscriptionname);
                return subscriptionClients.Create(description, factory);
            }
            catch (SubscriptionAlreadyInUseException)
            {
                // if this occurs, it means that another endpoint is using the same eventtype name but in another namespace,
                // so let's differenatiate including this namespace, odds are very likely that we will get a guid instead
                // that's why we're not defaulting to this convention.

                subscriptionname = NamingConventions.SubscriptionFullNamingConvention(config, eventType, config.Settings.EndpointName());
                var description = subscriptionCreator.Create(address, eventType, subscriptionname);
                return subscriptionClients.Create(description, factory);
            }

        }

        public void Unsubscribe(INotifyReceivedBrokeredMessages notifier)
        {
            var subscriptionname = NamingConventions.SubscriptionNamingConvention(config, notifier.MessageType, config.Settings.EndpointName());

            subscriptionCreator.Delete(notifier.Address, subscriptionname);
        }

        public INotifyReceivedBrokeredMessages GetReceiver(Address original)
        {
            var address = NamingConventions.QueueAddressConvention(config, original);
            var factory = messagingFactories.Create(address);
            var description = queueCreator.Create(address);
            var notifier = (AzureServiceBusQueueNotifier)config.Builder.Build(typeof(AzureServiceBusQueueNotifier));
            notifier.QueueClient = queueClients.Create(description, factory);

            //todo: notifier.BatchSize = maximumConcurrencyLevel;
            return notifier;
        }

        public ISendBrokeredMessages GetSender(Address address)
        {
            var factory = messagingFactories.Create(address);
            var description = queueCreator.Create(address);
            var sender = (AzureServiceBusQueueSender)config.Builder.Build(typeof(AzureServiceBusQueueSender));
            sender.QueueClient = queueClients.Create(description, factory);
            return sender;
        }

        public IPublishBrokeredMessages GetPublisher(Address original)
        {
            var address = NamingConventions.PublisherAddressConvention(config, original);
            var description = topicCreator.Create(address);
            var factory = messagingFactories.Create(address);
            var publisher = (AzureServiceBusTopicPublisher)config.Builder.Build(typeof(AzureServiceBusTopicPublisher));
            publisher.TopicClient = topicClients.Create(description, factory);
            return publisher;
        }

        public void Create(Address original)
        {
            var queue = NamingConventions.QueueAddressConvention(config, original);
            var topic = NamingConventions.PublisherAddressConvention(config, original);
            queueCreator.Create(queue);
            topicCreator.Create(topic);
        }
    }
}