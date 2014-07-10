using System.Transactions;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Transports;
    using Unicast;
    using Unicast.Queuing;

    /// <summary>
    /// 
    /// </summary>
    internal class AzureServiceBusSender : ISendMessages, IDeferMessages
    {
        readonly ITopology topology;
        readonly Configure config;

        public AzureServiceBusSender(ITopology topology, Configure config)
        {
            this.topology = topology;
            this.config = config;
        }

        public void Send(TransportMessage message, SendOptions options)
        {
            SendInternal(message, options);
        }

        public void Defer(TransportMessage message, SendOptions options)
        {
            SendInternal(message, options, expectDelay: true);
        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            //? throw new NotSupportedException();
        }

        void SendInternal(TransportMessage message, SendOptions options, bool expectDelay = false)
        {
            var sender = topology.GetSender(options.Destination);
       
            if (!config.Settings.Get<bool>("Transactions.Enabled") || Transaction.Current == null)
            {
                SendInternal(message, sender, options, expectDelay);
            }
            else
            {
                Transaction.Current.EnlistVolatile(new SendResourceManager(() => SendInternal(message, sender, options, expectDelay)), EnlistmentOptions.None);
            }
        }

        void SendInternal(TransportMessage message, ISendBrokeredMessages sender, SendOptions options, bool expectDelay)
        {
            try
            {
                using(var brokeredMessage = message.ToBrokeredMessage(options, config.Settings, expectDelay))
                {
                    sender.Send(brokeredMessage);
                }
            }
            catch (MessagingEntityNotFoundException)
            {
                throw new QueueNotFoundException
                {
                    Queue = options.Destination
                };
            }
        }

    }
}