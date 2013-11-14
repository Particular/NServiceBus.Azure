namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;

    public class AzureServicebusTopicCreator : ICreateTopics
    {
        public string Create(Address address)
        {
            var topicName = AzureServiceBusPublisherAddressConvention.Create(address);
            try
            {
                var namespaceclient = new CreatesNamespaceManagers().Create(address.Machine);
                if (!namespaceclient.TopicExists(topicName))
                {
                    namespaceclient.CreateTopic(topicName);
                }
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the topic already exists or another node beat us to it, which is ok
            }
            return topicName;
        }
    }
}