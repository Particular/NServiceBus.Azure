namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;

    class AzureServicebusQueueClientCreator : ICreateQueueClients
    {
        Configure config;

        public AzureServicebusQueueClientCreator(Configure config)
        {
            this.config = config;
        }

        public QueueClient Create(QueueDescription description, MessagingFactory factory)
        {
            var client = factory.CreateQueueClient(description.Path, ShouldRetry() ? ReceiveMode.PeekLock : ReceiveMode.ReceiveAndDelete);
            client.PrefetchCount = 100; // todo make configurable
            return client;
        }

        bool ShouldRetry()
        {
            return (bool)config.Settings.Get("Transactions.Enabled");
        }
    }
}