namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Transports;

    public class AzureServicebusTopicCreator : ICreateTopics
    {
        readonly ICreateNamespaceManagers createNamespaceManagers;

        public AzureServicebusTopicCreator() : this(new CreatesNamespaceManagers())
        {
        }

        public AzureServicebusTopicCreator(ICreateNamespaceManagers createNamespaceManagers)
        {
            this.createNamespaceManagers = createNamespaceManagers;
        }

        public void Create(Address address)
        {
            var topicName = address.Queue;
            try
            {
                var namespaceclient = createNamespaceManagers.Create(address.Machine);
                if (!namespaceclient.TopicExists(topicName))
                {
                    namespaceclient.CreateTopic(topicName);
                }
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the topic already exists or another node beat us to it, which is ok
            }
        }

        public void CreateIfNecessary(Address address)
        {
            Create(address);

        }
    }
}