namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Logging;
    using Transports;

    class AzureServicebusTopicCreator : ICreateTopics
    {
        ICreateNamespaceManagers createNamespaceManagers;
        Configure config;

        static ConcurrentDictionary<string, bool> rememberTopicExistence = new ConcurrentDictionary<string, bool>();

        public bool EnablePartitioning { get; set; }

        ILog logger = LogManager.GetLogger(typeof(AzureServicebusTopicCreator));

        public AzureServicebusTopicCreator(ICreateNamespaceManagers createNamespaceManagers, Configure config)
        {
            this.createNamespaceManagers = createNamespaceManagers;
            this.config = config;
        }

        public TopicDescription Create(Address address)
        {
            var topicName = address.Queue;
            var namespaceclient = createNamespaceManagers.Create(address.Machine);
            var description = new TopicDescription(topicName)
            {
                // todo: add the other settings from a separate config section? Or same as queue section?
                EnablePartitioning = EnablePartitioning
            };

            try
            {
                if (config.CreateQueues())
                {
                    if (!TopicExists(namespaceclient, topicName))
                    {
                        namespaceclient.CreateTopic(description);
                        logger.InfoFormat("Topic '{0}' created", description.Path);
                    }
                    else
                    {
                        logger.InfoFormat("Topic '{0}' already exists, skipping creation", description.Path);
                    }
                }
                else
                {
                    logger.InfoFormat("Create queues is set to false, skipping the creation of '{0}'", description.Path);
                }
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the topic already exists or another node beat us to it, which is ok
                logger.InfoFormat("Topic '{0}' already exists, another node probably beat us to it", description.Path);
            }
            catch (TimeoutException)
            {
                logger.InfoFormat("Timeout occured on topic creation for '{0}' going to validate if it doesn't exist", description.Path);

                // there is a chance that the timeout occurs, but the queue is created still
                // check for this
                if (!TopicExists(namespaceclient, topicName))
                {
                    throw;
                }
                else
                {
                    logger.InfoFormat("Looks like topic '{0}' exists anyway", description.Path);
                }
            }

            return description;
        }

        bool TopicExists(NamespaceManager namespaceClient, string topicpath)
        {
            var key = topicpath;
            logger.InfoFormat("Checking existence cache for existance of the topic '{0}'", topicpath);
            var exists = rememberTopicExistence.GetOrAdd(key, s =>
            {
                logger.InfoFormat("Checking namespace for existance of the topic '{0}'", topicpath);
                return namespaceClient.TopicExists(key);
            });

            logger.InfoFormat("Determined that the topic '{0}' {1}", topicpath, exists ? "exists" : "does not exist");

            return exists;
        }

    }
}