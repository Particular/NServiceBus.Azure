using System.Transactions;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using NServiceBus.Transports;
    using Unicast;
    using Unicast.Queuing;

    /// <summary>
    /// 
    /// </summary>
    internal class AzureServiceBusPublisher : IPublishMessages
    {
        readonly Configure config;
        readonly ITopology topology;

        public AzureServiceBusPublisher(Configure config, ITopology topology)
        {
            this.config = config;
            this.topology = topology;
        }

        public void Publish(TransportMessage message, PublishOptions options)
        {
            var publisher = topology.GetPublisher(config.LocalAddress);

            if (publisher == null) throw new QueueNotFoundException { Queue = config.LocalAddress };

            if (!config.Settings.Get<bool>("Transactions.Enabled") || Transaction.Current == null)
                Publish(publisher, message, options);
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(() => Publish(publisher, message, options)), EnlistmentOptions.None);
        }

        private void Publish(IPublishBrokeredMessages publisher, TransportMessage message, PublishOptions options)
        {
            using (var brokeredMessage = message.ToBrokeredMessage(options, config.Settings))
            {
                publisher.Publish(brokeredMessage);
            }
        }
    }
}