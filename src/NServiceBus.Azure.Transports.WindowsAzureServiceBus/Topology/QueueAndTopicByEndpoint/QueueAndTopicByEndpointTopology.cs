namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.QueueAndTopicByEndpoint
{
    using System;
    using Config;
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

        internal QueueAndTopicByEndpointTopology(
            Configure config, 
            ICreateMessagingFactories messagingFactories,
            ICreateNamespaceManagers namespaceManagers,
            ICreateSubscriptions subscriptionCreator, 
            ICreateQueues queueCreator,
            ICreateTopics topicCreator, 
            ICreateQueueClients queueClients)
        {
            this.config = config;
            this.messagingFactories = messagingFactories;
            this.namespaceManagers = namespaceManagers;
            this.subscriptionCreator = subscriptionCreator;
            this.queueCreator = queueCreator;
            this.topicCreator = topicCreator;
            this.queueClients = queueClients;
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
            //todo: notifier.BatchSize = maximumConcurrencyLevel;
            notifier.MessageType = eventType;
            notifier.Address = publisherAddress;
            return notifier;
        }

        public void Unsubscribe(INotifyReceivedBrokeredMessages notifier)
        {
            var subscriptionname = SubscriptionNamingConvention(notifier.MessageType, config.Settings.EndpointName());

            subscriptionCreator.Delete(notifier.Address, subscriptionname);
        }

        public INotifyReceivedBrokeredMessages GetReceiver(Address address)
        {
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

        public IPublishBrokeredMessages GetPublisher()
        {
            throw new NotImplementedException();
        }

    }
}