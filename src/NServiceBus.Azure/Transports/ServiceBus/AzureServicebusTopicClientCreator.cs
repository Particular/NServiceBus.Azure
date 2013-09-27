namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus.Messaging;

    public class AzureServicebusTopicClientCreator : ICreateTopicClients
    {
        public MessagingFactory Factory { get; set; }
        public ICreateTopics TopicCreator { get; set; }

        public TopicClient Create(Address address)
        {
            var topicName = TopicCreator.Create(address);

            return Factory.CreateTopicClient(topicName);
        }
    }
}