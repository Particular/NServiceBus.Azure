namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.Bundle
{
    using System;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Azure.Transports.WindowsAzureServiceBus.Transports;
    using NServiceBus.Logging;
    using NServiceBus.Settings;

    class BundleTopology : ITopology
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
        Random random = new Random();

        ILog logger = LogManager.GetLogger(typeof(BundleTopology));

        // find a home for these configuration settings

        string primaryConnectionString = "Endpoint=sb://topobybundle1.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=F+2isy9V5X2gzwy01NL261ljAIxF7BJ2PSi0518AkI4=";
        string secondaryConnectionString = "Endpoint=sb://topobybundle2.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=V1RuMaV3H08MfvXkPEcKWoUhVOfmuc806Mfign74a7M=";
        int numberOfPartitions = 2;
        string partitionPrefix = "partition";

        internal BundleTopology(
            Configure config, 
            IManageMessagingFactoriesLifecycle messagingFactories,
            ICreateSubscriptions subscriptionCreator, 
            ICreateQueues queueCreator,
            ICreateTopics topicCreator,
            IManageQueueClientsLifecycle queueClients, 
            ICreateSubscriptionClients subscriptionClients,
            IManageTopicClientsLifecycle topicClients, 
            ICreateQueueClients queueClientCreator)
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
        }

        public void Initialize(ReadOnlySettings setting)
        {
        }

        public INotifyReceivedBrokeredMessages Subscribe(Type eventType, Address address)
        {
            var queueName = NamingConventions.SubscriptionNamingConvention(config.Settings, address.Queue);
            var namespaces = new[]
            {
                primaryConnectionString,
                secondaryConnectionString
            };

            for (var i = 0; i < numberOfPartitions; i++)
            {
                var topic = NamingConventions.TopicNamingConvention(config.Settings, partitionPrefix, i);

                foreach (var ns in namespaces)
                {
                    var fact = messagingFactories.Get(topic, ns);
                    var desc = subscriptionCreator.Create(topic, ns, eventType, queueName, queueName);
                    var client = subscriptionClients.Create(desc, fact);
                    AddFilter(client, eventType);
                }
            }

            var notifier = new EmptyNotifier
            {
                MessageType = eventType,
                EntityName = queueName,
                Namespaces = namespaces
            };
            return notifier;
        }

        public void Unsubscribe(INotifyReceivedBrokeredMessages notifier)
        {
            for (var i = 0; i < numberOfPartitions; i++)
            {
                var topic = NamingConventions.TopicNamingConvention(config.Settings, partitionPrefix, i);

                foreach (var ns in notifier.Namespaces)
                {
                    subscriptionCreator.Delete(topic, ns, notifier.EntityName);
                }
            }
        }

        public INotifyReceivedBrokeredMessages GetReceiver(Address address)
        {
            var queueName = NamingConventions.QueueNamingConvention(config.Settings, address.Queue, false);
            
            var notifier = (AzureServiceBusQueueNotifier) config.Builder.Build(typeof(AzureServiceBusQueueNotifier));
            notifier.EntityName = queueName;
            notifier.Namespaces = new[] { primaryConnectionString, secondaryConnectionString };
            notifier.QueueClientFactory = (q, n) =>
            {
                var factory = messagingFactories.Get(q, n);
                return queueClientCreator.Create(q, factory);
            };
            return notifier;
        }

        public ISendBrokeredMessages GetSender(Address destination)
        {
            var selectedNamespace = SelectedRandomNamespace(random);

            var sender = (AzureServiceBusQueueSender)config.Builder.Build(typeof(AzureServiceBusQueueSender));
            sender.QueueClient = queueClients.Get(destination.Queue, selectedNamespace);
            return sender;
        }

        public IPublishBrokeredMessages GetPublisher(Address local)
        {
            var selectedNamespace = SelectedRandomNamespace(random);

            var selectedPartition = random.Next(0, numberOfPartitions);
            var topicName = NamingConventions.TopicNamingConvention(config.Settings, partitionPrefix, selectedPartition);

            var publisher = (AzureServiceBusTopicPublisher)config.Builder.Build(typeof(AzureServiceBusTopicPublisher));
            publisher.TopicClient = topicClients.Get(topicName, selectedNamespace);
            return publisher;
        }

        string SelectedRandomNamespace(Random rand)
        {
            string selectedNamespace;

            if (rand.Next(0, 2) == 0)
            {
                selectedNamespace = primaryConnectionString;
            }
            else
            {
                selectedNamespace = secondaryConnectionString;
            }
            return selectedNamespace;
        }

        public void Create(Address address)
        {
            logger.InfoFormat("Going to create queue for address '{0}' if needed", address.Queue);

            var queue = NamingConventions.QueueNamingConvention(config.Settings, address.Queue, false);
            queueCreator.Create(queue, primaryConnectionString);
            queueCreator.Create(queue, secondaryConnectionString);

            logger.InfoFormat("Going to create bundle of topics for partitions if needed", address.Queue);
            
            for(var i = 0; i < numberOfPartitions; i++)
            {
                var topic = NamingConventions.TopicNamingConvention(config.Settings, partitionPrefix, i);
                topicCreator.Create(topic, primaryConnectionString);
                topicCreator.Create(topic, secondaryConnectionString);
            }
        }

        private void AddFilter(SubscriptionClient subscriptionClient, Type eventType)
        {
            var filter = "1=1";
            var n = "$Default";

            if (eventType != null)
            {
                filter = new ServicebusSubscriptionFilterBuilder().BuildFor(eventType);
                n = NamingConventions.SqlFilterNamingConvention(config.Settings, eventType.FullName);
            }

            subscriptionClient.RemoveRule(n);
            subscriptionClient.AddRule(n, new SqlFilter(filter));
        }
    }
}
