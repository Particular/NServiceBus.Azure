using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using NServiceBus.Transports;
    using Settings;

    public class AzureServicebusQueueClientCreator : ICreateQueueClients
    {
        readonly ICreateQueues queueCreator;
        readonly ICreateMessagingFactories createMessagingFactories;

        public int MaxRetries { get; set; }

        public AzureServicebusQueueClientCreator(ICreateQueues queueCreator, ICreateMessagingFactories createMessagingFactories)
        {
            this.queueCreator = queueCreator;
            this.createMessagingFactories = createMessagingFactories;
        }

        public QueueClient Create(Address address)
        {
            if (QueueAutoCreation.ShouldAutoCreate)
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
            var config = Configure.Instance;// todo: inject

            return (bool)config.Settings.Get("Transactions.Enabled");
        }
    }
}