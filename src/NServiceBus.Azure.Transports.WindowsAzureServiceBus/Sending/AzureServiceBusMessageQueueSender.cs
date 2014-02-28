using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using NServiceBus.Transports;
    using Settings;
    using Unicast.Queuing;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusMessageQueueSender : ISendMessages
    {
        public const int DefaultBackoffTimeInSeconds = 10;

        private readonly Dictionary<string, QueueClient> senders = new Dictionary<string, QueueClient>();
        
        private static readonly object SenderLock = new Object();

        public int MaxDeliveryCount { get; set; }

        ICreateMessagingFactories createMessagingFactories;

        public AzureServiceBusMessageQueueSender(ICreateMessagingFactories createMessagingFactories)
        {
            this.createMessagingFactories = createMessagingFactories;
        }

        public void Send(TransportMessage message, string destination)
        {
            Send(message, Address.Parse(destination));
        }

        public void Send(TransportMessage message, Address address)
        {
            var destination = address.Queue;
            var @namespace = address.Machine;

            QueueClient sender;
            if (!senders.TryGetValue(destination, out sender))
            {
                lock (SenderLock)
                {
                    if (!senders.TryGetValue(destination, out sender))
                    {
                        var factory = createMessagingFactories.Create(@namespace);
                        sender = factory.CreateQueueClient(destination);
                        senders[destination] = sender;
                    }
                }
            }

            if (!SettingsHolder.Get<bool>("Transactions.Enabled") || Transaction.Current == null)
                Send(message, sender,address);
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(() => Send(message, sender, address)), EnlistmentOptions.None);

        }

        void Send(TransportMessage message, QueueClient sender, Address address)
        {
            var numRetries = 0;
            var sent = false;

            while (!sent)
            {
                try
                {
                    using (var brokeredMessage = message.Body != null ? new BrokeredMessage(message.Body) : new BrokeredMessage())
                    {
                        brokeredMessage.CorrelationId = message.CorrelationId;
                        if (message.TimeToBeReceived < TimeSpan.MaxValue) brokeredMessage.TimeToLive = message.TimeToBeReceived;

                        foreach (var header in message.Headers)
                        {
                            brokeredMessage.Properties[header.Key] = header.Value;
                        }

                        brokeredMessage.Properties[Headers.MessageIntent] = message.MessageIntent.ToString();
                        brokeredMessage.MessageId = message.Id;

                        if (message.ReplyToAddress != null)
                        {
                            brokeredMessage.ReplyTo = new DeterminesBestConnectionStringForAzureServiceBus().Determine(message.ReplyToAddress);
                        }

                        if (message.TimeToBeReceived < TimeSpan.MaxValue)
                        {
                            brokeredMessage.TimeToLive = message.TimeToBeReceived;
                        }

                        sender.Send(brokeredMessage);
                        sent = true;
                    }
                }
                catch (MessagingEntityNotFoundException)
                {
                    throw new QueueNotFoundException { Queue = address };
                }
                // todo: outbox
                catch (MessagingEntityDisabledException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
                // back off when we're being throttled
                catch (ServerBusyException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
                // connection lost
                catch (MessagingCommunicationException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
                // took to long, maybe we lost connection
                catch (TimeoutException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
            }
        }

      
   }
}