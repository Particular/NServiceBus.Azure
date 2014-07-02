namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.QueueAndTopicByEndpoint
{
    using System;
    using Config;
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
        readonly ICreateNamespaceManagers namespaceManagers;
        readonly ICreateSubscriptions subscriptionCreator;
        readonly ICreateQueues queueCreator;
        readonly ICreateTopics topicCreator;
        readonly ICreateQueueClients queueClients; 
        readonly ICreateSubscriptionClients subscriptionClients;
        readonly ICreateTopicClients topicClients;

        internal QueueAndTopicByEndpointTopology(
            Configure config, 
            ICreateMessagingFactories messagingFactories,
            ICreateNamespaceManagers namespaceManagers,
            ICreateSubscriptions subscriptionCreator, 
            ICreateQueues queueCreator,
            ICreateTopics topicCreator, 
            ICreateQueueClients queueClients, 
            ICreateSubscriptionClients subscriptionClients,
            ICreateTopicClients topicClients)
        {
            this.config = config;
            this.messagingFactories = messagingFactories;
            this.namespaceManagers = namespaceManagers;
            this.subscriptionCreator = subscriptionCreator;
            this.queueCreator = queueCreator;
            this.topicCreator = topicCreator;
            this.queueClients = queueClients;
            this.subscriptionClients = subscriptionClients;
            this.topicClients = topicClients;
        }

        public Func<Type, string, string> QueueNamingConvention {
            get
            {
                return (messagetype, endpointname) =>
                {
                    var queueName = endpointname;

                    var configSection = config != null ? config.Settings.GetConfigSection<AzureServiceBusQueueConfig>() : null;

                    if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
                    {
                        queueName = configSection.QueueName;
                    }

                    if (queueName.Length >= 283) // 290 - a spot for the "-" & 6 digits for the individualizer
                        queueName = new DeterministicGuidBuilder().Build(queueName).ToString();

                    if (config != null && !config.Settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                        queueName = QueueIndividualizer.Individualize(queueName);

                    return queueName;
                };
            }
        }

        public Func<Type, string, string> SubscriptionNamingConvention
        {
            get
            {
                return (messagetype, endpointname) =>
                {
                    var subscriptionName = messagetype != null ? endpointname + "." + messagetype.Name : endpointname;

                    if (subscriptionName.Length >= 50)
                        subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

                    return subscriptionName;
                };
            }
        }
        public Func<Type, string, string> SubscriptionFullNamingConvention
        {
            get
            {
                return (messagetype, endpointname) =>
                {
                    var subscriptionName = messagetype != null ? endpointname + "." + messagetype.FullName : endpointname;

                    if (subscriptionName.Length >= 50)
                        subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

                    return subscriptionName;
                };
            }
        }

        public Func<Type, string, string> TopicNamingConvention
        {
            get
            {
                return (messagetype, endpointname) =>
                {
                    var name = endpointname;

                    if (name.Length >= 290)
                        name = new DeterministicGuidBuilder().Build(name).ToString();

                    return name;
                };
            }
        }
        public Func<Address, Address> PublisherAddressConvention
        {
            get
            {
                return address => Address.Parse(TopicNamingConvention(null, address.Queue + ".events") + "@" + address.Machine);
            }
        }
        public Func<Address, Address> PublisherAddressConventionForSubscriptions
        {
            get { return PublisherAddressConvention; }
        }
        public Func<Address, Address> QueueAddressConvention
        {
            get
            {
                return address => Address.Parse(QueueNamingConvention(null, address.Queue) + "@" + address.Machine);
            }
        }

        public void Initialize(ReadOnlySettings settings)
        {
            var queuename = QueueNamingConvention(null, settings.EndpointName());
            Address.InitializeLocalAddress(queuename);
        }

        public void Create()
        {
            throw new NotImplementedException();
        }

        public INotifyReceivedBrokeredMessages Subscribe(Type eventType, Address address)
        {
            var publisherAddress = PublisherAddressConventionForSubscriptions(address);
            var notifier = config.Builder.Build<AzureServiceBusSubscriptionNotifier>();
            notifier.SubscriptionClient = CreateSubscriptionClient(eventType, publisherAddress);
            //todo: notifier.BatchSize = maximumConcurrencyLevel;
            notifier.MessageType = eventType;
            notifier.Address = publisherAddress;
            return notifier;
        }

        private SubscriptionClient CreateSubscriptionClient(Type eventType, Address address)
        {
            var subscriptionname = SubscriptionNamingConvention(eventType, config.Settings.EndpointName());
            var factory = messagingFactories.Create(address.Machine);

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

                subscriptionname = SubscriptionFullNamingConvention(eventType, config.Settings.EndpointName());
                var description = subscriptionCreator.Create(address, eventType, subscriptionname);
                return subscriptionClients.Create(description, factory);
            }

        }

        public void Unsubscribe(INotifyReceivedBrokeredMessages notifier)
        {
            var subscriptionname = SubscriptionNamingConvention(notifier.MessageType, config.Settings.EndpointName());

            subscriptionCreator.Delete(notifier.Address, subscriptionname);
        }

        public INotifyReceivedBrokeredMessages GetReceiver(Address original)
        {
            var address = QueueAddressConvention(original);
            var factory = messagingFactories.Create(address.Machine);
            var description = queueCreator.Create(address);
            var notifier = (AzureServiceBusQueueNotifier)config.Builder.Build(typeof(AzureServiceBusQueueNotifier));
            notifier.QueueClient = queueClients.Create(description, factory);

            //todo: notifier.BatchSize = maximumConcurrencyLevel;
            return notifier;
        }

        public ISendBrokeredMessages GetSender()
        {
            throw new NotImplementedException();
        }

        public IPublishBrokeredMessages GetPublisher(Address original)
        {
            var address = PublisherAddressConvention(original);
            var description = topicCreator.Create(address);
            var factory = messagingFactories.Create(address.Machine);
            var publisher = (AzureServiceBusTopicPublisher)config.Builder.Build(typeof(AzureServiceBusTopicPublisher));
            publisher.TopicClient = topicClients.Create(description, factory);
            return publisher;
        }

    }

}