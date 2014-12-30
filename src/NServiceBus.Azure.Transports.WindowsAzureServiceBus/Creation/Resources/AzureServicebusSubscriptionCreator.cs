namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Azure.Transports.WindowsAzureServiceBus.Transports;
    using NServiceBus.Logging;

    class AzureServiceBusSubscriptionCreator : ICreateSubscriptions
    {
        public TimeSpan LockDuration { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }
        public bool EnableDeadLetteringOnFilterEvaluationExceptions { get; set; }

        ICreateNamespaceManagers createNamespaceManagers;
        Configure config;
        readonly ICreateTopics topicCreator;

        static ConcurrentDictionary<string, bool> rememberTopicExistence = new ConcurrentDictionary<string, bool>();
        static ConcurrentDictionary<string, bool> rememberSubscriptionExistence = new ConcurrentDictionary<string, bool>();
     
        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusSubscriptionCreator));

        public AzureServiceBusSubscriptionCreator(ICreateNamespaceManagers createNamespaceManagers, Configure config, ICreateTopics topicCreator)
        {
            this.createNamespaceManagers = createNamespaceManagers;
            this.config = config;
            this.topicCreator = topicCreator;
        }

        public SubscriptionDescription Create(string topicName, string @namespace, Type eventType, string subscriptionname, string forwardTo = null)
        {
            var namespaceClient = createNamespaceManagers.Create(@namespace);

            var description = new SubscriptionDescription(topicName, subscriptionname)

            {
                // same as queue section from AzureServiceBusQueueConfig
                LockDuration = LockDuration,
                RequiresSession = RequiresSession,
                DefaultMessageTimeToLive = DefaultMessageTimeToLive,
                EnableDeadLetteringOnMessageExpiration = EnableDeadLetteringOnMessageExpiration,
                MaxDeliveryCount = MaxDeliveryCount,
                EnableBatchedOperations = EnableBatchedOperations,
                EnableDeadLetteringOnFilterEvaluationExceptions = EnableDeadLetteringOnFilterEvaluationExceptions,
                ForwardTo = forwardTo
            };

            if (config.CreateQueues())
            {
                if (!TopicExists(namespaceClient, topicName))
                {
                    logger.Info(string.Format("The topic that you're trying to subscribe to, {0}, doesn't exist yet, going to create it...", topicName));
                    topicCreator.Create(topicName, namespaceClient);
                }
                
                try
                {
                    if (!SubscriptionExists(namespaceClient, topicName, subscriptionname))
                    {
                       namespaceClient.CreateSubscription(description);
                       
                       logger.InfoFormat("Subscription '{0}' on topic '{1}' created", description.Name, topicName);
                    }
                    else
                    {
                        logger.InfoFormat("Subscription '{0}' on topic '{1}' already exists, skipping creation", description.Name, topicName);
                    }
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    // the queue already exists or another node beat us to it, which is ok
                    logger.InfoFormat("Subscription '{0}' on topic '{1}' already exists, another node probably beat us to it", description.Name, topicName);
                }
                catch (TimeoutException)
                {
                    logger.InfoFormat("Timeout occured on creation of subscription '{0}' on topic '{1}', going to validate if it doesn't exists", description.Name, topicName);

                    // there is a chance that the timeout occurs, but the subscription is created still
                    // check for this
                    if (!namespaceClient.SubscriptionExists(topicName, subscriptionname))
                    {
                        throw;
                    }
                    else
                    {
                        logger.InfoFormat("Looks like subscription '{0}' on topic '{1}' exists anyway", description.Name, topicName);
                    }
                }
                catch (MessagingException ex)
                {
                    if (!ex.IsTransient && !CreationExceptionHandling.IsCommon(ex))
                    {
                        logger.Fatal(string.Format("{2} {3} occured on subscription creation {0} on topic '{1}'", description.Name, topicName, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);
                        throw;
                    }
                    else
                    {
                        logger.Info(string.Format("{2} {3} occured on subscription creation {0} on topic '{1}'", description.Name, topicName, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);
                    }
                }
            }
            return description;
        }


        public void Delete(string topicName, string @namespace, string subscriptionname)
        {
            var namespaceClient = createNamespaceManagers.Create(@namespace);
            if (SubscriptionExists(namespaceClient, topicName, subscriptionname))
            {
                namespaceClient.DeleteSubscription(topicName, subscriptionname);
            }
        }

        bool TopicExists(NamespaceManager namespaceClient, string topicpath)
        {
            var key = topicpath;
            logger.InfoFormat("Checking existence cache for topic '{0}'", topicpath);
            var exists = rememberTopicExistence.GetOrAdd(key, s => {
                    logger.InfoFormat("Checking namespace for existance of the topic '{0}'", topicpath);
                    return namespaceClient.TopicExists(topicpath);
            });
            logger.InfoFormat("Determined that the topic '{0}' {1}", topicpath, exists ? "exists" : "does not exist");
            return exists;
        }

        bool SubscriptionExists(NamespaceManager namespaceClient, string topicpath, string subscriptionname)
        {
            var key = topicpath + subscriptionname;
            logger.InfoFormat("Checking cache for existence of subscription '{0}' on topic '{1}'", subscriptionname, topicpath);
            var exists = rememberSubscriptionExistence.GetOrAdd(key, s => {
                logger.InfoFormat("Checking namespace for subscription '{0}' on  topic '{1}'", subscriptionname, topicpath);
                return namespaceClient.SubscriptionExists(topicpath, subscriptionname);
            });
           
            logger.InfoFormat("Determined cache that the subscription '{0}' on topic '{1}' {2}", subscriptionname, topicpath, exists ? "exists" : "does not exist");

            return exists;
        }
    }
}