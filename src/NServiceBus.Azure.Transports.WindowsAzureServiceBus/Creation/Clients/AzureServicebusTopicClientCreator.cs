namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;
    using Transports;

    public class AzureServicebusTopicClientCreator : ICreateTopicClients
    {
        readonly ICreateMessagingFactories createMessagingFactories;
        readonly ICreateTopics topicCreator;
        readonly ITopology topology;

        public bool ShouldAutoCreate { get; set; }

        public AzureServicebusTopicClientCreator(ICreateMessagingFactories createMessagingFactories, ICreateTopics topicCreator, ITopology topology)
        {
            this.createMessagingFactories = createMessagingFactories;
            this.topicCreator = topicCreator;
            this.topology = topology;
        }

        public TopicClient Create(Address address)
        {
            address = topology.PublisherAddressConvention(address);

            if (ShouldAutoCreate) // todo move to property
            {
                topicCreator.Create(address);
            }

            var factory = createMessagingFactories.Create(address.Machine);
            return factory.CreateTopicClient(address.Queue);
        }
    }
}