using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using NServiceBus.Transports;

    public class AzureServicebusQueueClientCreator : ICreateQueueClients
    {
        readonly ICreateQueues queueCreator;
        readonly ICreateMessagingFactories createMessagingFactories;
        readonly Configure config;

        public int MaxRetries { get; set; }
        public bool ShouldAutoCreate { get; set; }

        public AzureServicebusQueueClientCreator(ICreateQueues queueCreator, ICreateMessagingFactories createMessagingFactories, Configure config)
        {
            this.queueCreator = queueCreator;
            this.createMessagingFactories = createMessagingFactories;
            this.config = config;
        }

        public QueueClient Create(Address address)
        {
            if (ShouldAutoCreate) 
            {
                queueCreator.CreateQueueIfNecessary(address, null);
            }

            var factory = createMessagingFactories.Create(address.Machine);
            var client = factory.CreateQueueClient(address.Queue, ShouldRetry() ? ReceiveMode.PeekLock : ReceiveMode.ReceiveAndDelete);
            client.PrefetchCount = 100; // todo make configurable
            return client;
        }

        bool ShouldRetry()
        {
            return (bool)config.Settings.Get("Transactions.Enabled");
        }
    }
}