namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.QueueAndTopicByEndpoint
{
    using System;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Logging;
    using Settings;
    using Transports;

    /// <summary>
    /// Sends occur through queues, one for each endpoint, 
    /// publishes through a topic per endpoint, 
    /// receives on both it's own queue &amp; subscriptions per datatype
    /// </summary>
    class QueueAndTopicByEndpointTopology : ITopology
    {
        Configure config;
        IManageMessagingFactoriesLifecycle messagingFactories;
        ICreateSubscriptions subscriptionCreator;
        ICreateQueues queueCreator;
        ICreateTopics topicCreator;
        IManageQueueClientsLifecycle queueClients; 
        ICreateSubscriptionClients subscriptionClients;
        IManageTopicClientsLifecycle topicClients;
        ICreateQueueClients queueClientCreator;
        readonly ICreateNamespaceManagers createNamespaceManagers;

        ILog logger = LogManager.GetLogger(typeof(QueueAndTopicByEndpointTopology));

        internal QueueAndTopicByEndpointTopology(
            Configure config, 
            IManageMessagingFactoriesLifecycle messagingFactories,
            ICreateSubscriptions subscriptionCreator, 
            ICreateQueues queueCreator,
            ICreateTopics topicCreator,
            IManageQueueClientsLifecycle queueClients, 
            ICreateSubscriptionClients subscriptionClients,
            IManageTopicClientsLifecycle topicClients, 
            ICreateQueueClients queueClientCreator,
            ICreateNamespaceManagers createNamespaceManagers)
        {
            this.config = config;
            this.messagingFactories = messagingFactories;
            this.subscriptionCreator = subscriptionCreator;
            this.queueCreator = queueCreator;
            this.topicCreator = topicCreator;
            this.queueClients = queueClients;
            this.subscriptionClients = subscriptionClients;
            this.topicClients = topicClients;
            this.queueClientCreator = queueClientCreator;
            this.createNamespaceManagers = createNamespaceManagers;
        }

        public void Initialize(ReadOnlySettings settings)
        {
            
        }

        public INotifyReceivedBrokeredMessages Subscribe(Type eventType, Address address)
        {
            var publisherAddress = NamingConventions.PublisherAddressConventionForSubscriptions(config.Settings, address);
            var notifier = config.Builder.Build<AzureServiceBusSubscriptionNotifier>();
            notifier.SubscriptionClientFactory = CreateSubscriptionClient;
            //todo: notifier.BatchSize = maximumConcurrencyLevel;
            notifier.MessageType = eventType;
            notifier.EntityName = publisherAddress.Queue;
            notifier.Namespaces = new []{ publisherAddress.Machine };
            return notifier;
        }

        SubscriptionClient CreateSubscriptionClient(Type eventType, string topicName, string @namespace)
        {
            var subscriptionname = NamingConventions.SubscriptionNamingConvention(config.Settings, eventType, config.Settings.EndpointName());
            var factory = messagingFactories.Get(topicName, @namespace);
            var ns = createNamespaceManagers.Create(@namespace);
            SubscriptionClient client;

            try
            {
                var description = subscriptionCreator.Create(topicName, @namespace, eventType, subscriptionname);
                client = subscriptionClients.Create(description, factory);
            }
            catch (SubscriptionAlreadyInUseException)
            {
                // if this occurs, it means that another endpoint is using the same eventtype name but in another namespace,
                // so let's differenatiate including this namespace, odds are very likely that we will get a guid instead
                // that's why we're not defaulting to this convention.

                subscriptionname = NamingConventions.SubscriptionFullNamingConvention(config.Settings, eventType, config.Settings.EndpointName());
                var description = subscriptionCreator.Create(topicName, @namespace, eventType, subscriptionname);
                client = subscriptionClients.Create(description, factory);
            }
            AddFilter(client, eventType, subscriptionname, ns, topicName);
            return client;
        }

        public void Unsubscribe(INotifyReceivedBrokeredMessages notifier)
        {
            var subscriptionname = NamingConventions.SubscriptionNamingConvention(config.Settings, notifier.MessageType, config.Settings.EndpointName());

            foreach (var ns in notifier.Namespaces)
            {
                subscriptionCreator.Delete(notifier.EntityName, ns, subscriptionname);
            }
        }

        public INotifyReceivedBrokeredMessages GetReceiver(Address original)
        {
            var address = NamingConventions.QueueAddressConvention(config.Settings, original, false);

            var notifier = (AzureServiceBusQueueNotifier)config.Builder.Build(typeof(AzureServiceBusQueueNotifier));
            notifier.EntityName = address.Queue;
            notifier.Namespaces = new[] { address.Machine };
            notifier.QueueClientFactory = (q, n) =>
            {
                var desc = queueCreator.Create(q, n); //we shouldn't do this over and over
                var factory = messagingFactories.Get(q, n);
                return queueClientCreator.Create(desc, factory);
            };

            return notifier;
        }

        public ISendBrokeredMessages GetSender(Address original)
        {
            var address = NamingConventions.QueueAddressConvention(config.Settings, original, true);
            queueCreator.Create(address.Queue, address.Machine); //we shouldn't do this over and over
            var sender = (AzureServiceBusQueueSender)config.Builder.Build(typeof(AzureServiceBusQueueSender));
            sender.QueueClient = queueClients.Get(address.Queue, address.Machine);
            return sender;
        }

        public IPublishBrokeredMessages GetPublisher(Address original)
        {
            var address = NamingConventions.PublisherAddressConvention(config.Settings, original);
            topicCreator.Create(address.Queue, address.Machine); //we shouldn't do this over and over
            var publisher = (AzureServiceBusTopicPublisher)config.Builder.Build(typeof(AzureServiceBusTopicPublisher));
            publisher.TopicClient = topicClients.Get(address.Queue, address.Machine);
            return publisher;
        }

        public void Create(Address original)
        {
            logger.InfoFormat("Going to create queue for address '{0}' if needed", original.Queue);

            var queue = NamingConventions.QueueAddressConvention(config.Settings, original, false);
            queueCreator.Create(queue.Queue, queue.Machine);

            logger.InfoFormat("Going to create topic for address '{0}' if needed", original.Queue);
            if (original == config.LocalAddress)
            {
                var topic = NamingConventions.PublisherAddressConvention(config.Settings, original);
                topicCreator.Create(topic.Queue, topic.Machine);
            }
            else
            {
                logger.InfoFormat("Did not create topic for address  '{0}' as it does not correspond to the local address", original.Queue);
            }
        }


        private void AddFilter(SubscriptionClient subscriptionClient, Type eventType, string subscriptionname, NamespaceManager namespaceClient, string topicPath)
        {
            var filter = "1=1";
            var n = "$Default";

            if (eventType != null)
            {
                filter = new ServicebusSubscriptionFilterBuilder().BuildFor(eventType);
            }

            GuardAgainstSubscriptionReuseAcrossLogicalEndpoints(subscriptionname, namespaceClient, topicPath, filter);

            subscriptionClient.RemoveRule(n);
            subscriptionClient.AddRule(n, new SqlFilter(filter));
        }

        private void GuardAgainstSubscriptionReuseAcrossLogicalEndpoints(string subscriptionname, NamespaceManager namespaceClient, string topicPath, string filter)
        {
            var rules = namespaceClient.GetRules(topicPath, subscriptionname);
            foreach (var rule in rules)
            {
                var sqlFilter = rule.Filter as SqlFilter;
                if (sqlFilter != null && sqlFilter.SqlExpression != "1=1" && sqlFilter.SqlExpression != filter)
                {
                    throw new SubscriptionAlreadyInUseException(
                        "Looks like this subscriptionname is already taken by another logical endpoint as the sql filter does not match the subscribed eventtype, please choose a different subscription name!");
                }
            }
        }
    }
}