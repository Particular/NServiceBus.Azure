namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class AzureServicebusTopicClientCreator : ICreateTopicClients
    {
        public MessagingFactory Factory { get; set; }
        public NamespaceManager NamespaceClient { get; set; }

        public TopicClient Create(Address address)
        {
            var topicName = CreateTopic(address);

            return Factory.CreateTopicClient(topicName);
        }

        public string CreateTopic(Address address)
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