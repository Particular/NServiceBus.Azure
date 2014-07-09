namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Transports;

    internal class AzureServicebusTopicCreator : ICreateTopics
    {
        readonly ICreateNamespaceManagers createNamespaceManagers;

        private static readonly Dictionary<string, bool> rememberTopicExistance = new Dictionary<string, bool>();
        private static readonly object TopicExistanceLock = new Object();

        public bool EnablePartitioning { get; set; }

        public AzureServicebusTopicCreator(ICreateNamespaceManagers createNamespaceManagers)
        {
            this.createNamespaceManagers = createNamespaceManagers;
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
                if (!ConfigureQueueCreation.DontCreateQueues)
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
            if (!rememberTopicExistance.ContainsKey(key))
            {
                lock (TopicExistanceLock)
                {
                    exists = namespaceClient.TopicExists(key);
                    rememberTopicExistance[key] = exists;
                }
            }
            else
            {
                exists = rememberTopicExistance[key];
            }

            return exists;
        }

    }
}