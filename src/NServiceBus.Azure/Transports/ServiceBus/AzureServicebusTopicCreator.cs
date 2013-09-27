namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class AzureServicebusTopicCreator : ICreateTopics
    {
        public NamespaceManager NamespaceClient { get; set; }

        public string Create(Address address)
        {
            var topicName = AzureServiceBusPublisherAddressConvention.Create(address);
            try
            {
                if (!NamespaceClient.TopicExists(topicName))
                {
                    NamespaceClient.CreateTopic(topicName);
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