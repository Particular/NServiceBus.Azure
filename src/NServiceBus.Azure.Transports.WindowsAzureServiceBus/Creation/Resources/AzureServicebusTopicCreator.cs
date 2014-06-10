namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Transports;

    public class AzureServicebusTopicCreator : ICreateTopics
    {
        readonly ICreateNamespaceManagers createNamespaceManagers;

        public bool EnablePartitioning { get; set; }

        public AzureServicebusTopicCreator(Configure config)
            : this(new CreatesNamespaceManagers(config))
        {
        }

        public AzureServicebusTopicCreator(ICreateNamespaceManagers createNamespaceManagers)
        {
            this.createNamespaceManagers = createNamespaceManagers;
        }

        public void Create(Address address)
        {
            var topicName = address.Queue;
            var namespaceclient = createNamespaceManagers.Create(address.Machine);
            try
            {
                
                if (!namespaceclient.TopicExists(topicName))
                {
                    var description = new TopicDescription(topicName)
                    {
                        // todo: add the other settings from a separate config section? Or same as queue section?
                        EnablePartitioning = EnablePartitioning
                    };

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
        }

        public void CreateIfNecessary(Address address)
        {
            Create(address);

        }
    }
}