using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System.Configuration;
    using Settings;
    using Unicast.Transport;

    internal class AzureServicebusGatewayQueueClientCreator : ICreateQueueClients
    {
        readonly AzureServiceBusGatewayQueueCreator queueCreator;
        readonly ICreateMessagingFactories createMessagingFactories;

        public int MaxRetries { get; set; }

        public AzureServicebusGatewayQueueClientCreator(AzureServiceBusGatewayQueueCreator queueCreator, ICreateMessagingFactories createMessagingFactories)
        {
            this.queueCreator = queueCreator;
            this.createMessagingFactories = createMessagingFactories;
        }

        public QueueClient Create(string address)
        {
            var @namespace = TransportConnectionString.GetConnectionStringOrNull("NServiceBus/Gateway/" + address);

            if (@namespace == null)
            {
                throw new ConfigurationErrorsException(string.Format("No connection string has been defined for the channel {0}", address));
            }

            if (GatewayQueueAutoCreation.ShouldAutoCreate)
            {
                queueCreator.Create(address, @namespace);
            }

            var factory = createMessagingFactories.Create(@namespace);
            var client = factory.CreateQueueClient(address, ShouldRetry() ? ReceiveMode.PeekLock : ReceiveMode.ReceiveAndDelete);
            client.PrefetchCount = 100; // todo make configurable
            return client;
        }

        bool ShouldRetry()
        {
            return (bool) SettingsHolder.Get("Transactions.Enabled");
        }
    }
}