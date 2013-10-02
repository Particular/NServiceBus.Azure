namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus.Messaging;

    public class AzureServicebusTopicClientCreator : ICreateTopicClients
    {
        public TopicClient Create(Address address)
        {
            var topicName = new AzureServicebusTopicCreator().Create(address);

            var factory = new CreatesMessagingFactories().Create(address.Machine);
            return factory.CreateTopicClient(topicName);
        }
    }
}