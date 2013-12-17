using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Features;
    using NServiceBus.Transports;

    public class AzureServicebusQueueClientCreator : ICreateQueueClients
    {
        readonly ICreateQueues queueCreator;
        readonly ICreateMessagingFactories createMessagingFactories;

        public AzureServicebusQueueClientCreator(ICreateQueues queueCreator, ICreateMessagingFactories createMessagingFactories)
        {
            this.queueCreator = queueCreator;
            this.createMessagingFactories = createMessagingFactories;
        }

        public QueueClient Create(Address address)
        {
            if (Feature.IsEnabled<QueueAutoCreation>() && !ConfigureQueueCreation.DontCreateQueues)
            {
                queueCreator.CreateQueueIfNecessary(address, null);
            }

            var factory = createMessagingFactories.Create(address.Machine);
            var client = factory.CreateQueueClient(address.Queue, ReceiveMode.PeekLock);
            client.PrefetchCount = 100; // todo make configurable
            return client;
        }

    }
}