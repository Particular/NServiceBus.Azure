namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus.Messaging;

    public class AzureServicebusTopicClientCreator : ICreateTopicClients
    {
        public ICreateTopics TopicCreator { get; set; }

        public TopicClient Create(Address address)
        {
            var topicName = TopicCreator.Create(address);

            var factory = new CreatesMessagingFactories().Create(address.Machine);
            return factory.CreateTopicClient(topicName);
        }
    }
}