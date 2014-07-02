namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Microsoft.ServiceBus.Messaging;
    using Transports;

    public class AzureServicebusTopicCreator : ICreateTopics
    {
        readonly ICreateNamespaceManagers createNamespaceManagers;

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
                
                if (!namespaceclient.TopicExists(topicName))
                {
                    

                    namespaceclient.CreateTopic(description);
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
                if (!namespaceclient.QueueExists(topicName))
                    throw;
            }

            return description;
        }

    }
}