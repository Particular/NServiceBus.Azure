namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Transports;

    internal class AzureServicebusTopicCreator : ICreateTopics
    {
        ICreateNamespaceManagers createNamespaceManagers;
        Configure config;

        private static Dictionary<string, bool> rememberTopicExistence = new Dictionary<string, bool>();
        private static object TopicExistenceLock = new Object();

        public bool EnablePartitioning { get; set; }

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
                    }
                }
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the topic already exists or another node beat us to it, which is ok
            }
            catch (TimeoutException)
            {
                // there is a chance that the timeout occurs, but the queue is created still
                // check for this
                if (!TopicExists(namespaceclient, topicName))
                    throw;
            }

            return description;
        }

        bool TopicExists(NamespaceManager namespaceClient, string topicpath)
        {
            var key = topicpath;
            bool exists;
            if (!rememberTopicExistence.ContainsKey(key))
            {
                lock (TopicExistenceLock)
                {
                    exists = namespaceClient.TopicExists(key);
                    rememberTopicExistence[key] = exists;
                }
            }
            else
            {
                exists = rememberTopicExistence[key];
            }

            return exists;
        }

    }
}