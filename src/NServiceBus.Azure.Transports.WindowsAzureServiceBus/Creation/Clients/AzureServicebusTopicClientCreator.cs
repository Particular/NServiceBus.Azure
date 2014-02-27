namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Transports;

    public class AzureServicebusTopicClientCreator : ICreateTopicClients
    {
        readonly ICreateMessagingFactories createMessagingFactories;
        readonly ICreateTopics topicCreator;

        public AzureServicebusTopicClientCreator(ICreateMessagingFactories createMessagingFactories, ICreateTopics topicCreator)
        {
            this.createMessagingFactories = createMessagingFactories;
            this.topicCreator = topicCreator;
        }

        public TopicClient Create(Address address)
        {
            address = AzureServiceBusPublisherAddressConvention.Apply(address);
            if (QueueAutoCreation.ShouldAutoCreate)
            {
                topicCreator.CreateIfNecessary(address);
            }

            var factory = createMessagingFactories.Create(address.Machine);
            return factory.CreateTopicClient(address.Queue);
        }
    }
}