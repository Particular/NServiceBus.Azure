using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using NServiceBus.Transports;
    using Unicast;
    using Unicast.Queuing;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusMessageQueueSender : ISendMessages, IDeferMessages
    {
        const int DefaultBackoffTimeInSeconds = 10;

        private readonly Dictionary<string, QueueClient> senders = new Dictionary<string, QueueClient>();
        
        private static readonly object SenderLock = new Object();

        public int MaxDeliveryCount { get; set; }

        ICreateMessagingFactories createMessagingFactories;

        public AzureServiceBusMessageQueueSender(ICreateMessagingFactories createMessagingFactories)
        {
            this.createMessagingFactories = createMessagingFactories;
        }

        public void Send(TransportMessage message, SendOptions options)
        {
            SendInternal(message, options);
        }

        public void Defer(TransportMessage message, SendOptions options)
        {
            SendInternal(message, options, expectDelay:true);
        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            //? throw new NotSupportedException();
        }

        void SendInternal(TransportMessage message, SendOptions options, bool expectDelay = false)
        {
            var address = options.Destination;
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
            var config = Configure.Instance; // todo: inject
            if (!config.Settings.Get<bool>("Transactions.Enabled") || Transaction.Current == null)
            {
                SendInternal(message, sender, options, expectDelay);
            }
            else
            {
                Transaction.Current.EnlistVolatile(new SendResourceManager(() => SendInternal(message, sender, options, expectDelay)), EnlistmentOptions.None);
            }
        }

        void SendInternal(TransportMessage message, QueueClient sender, SendOptions options, bool expectDelay )
        {
            var address = options.Destination;
            var timeToSend = DelayIfNeeded(options, expectDelay);

            var numRetries = 0;
            var sent = false;

            while (!sent)
            {
                try
                {
                    using (var brokeredMessage = message.Body != null ? new BrokeredMessage(message.Body) : new BrokeredMessage())
                    {
                        brokeredMessage.CorrelationId = message.CorrelationId;
                        
                        if (timeToSend.HasValue)
                            brokeredMessage.ScheduledEnqueueTimeUtc = timeToSend.Value;

                        foreach (var header in message.Headers)
                        {
                            brokeredMessage.Properties[header.Key] = header.Value;
                        }

                        brokeredMessage.Properties[Headers.MessageIntent] = message.MessageIntent.ToString();
                        brokeredMessage.MessageId = message.Id;

                        if (options.ReplyToAddress != null)
                        {
                            brokeredMessage.ReplyTo = new DeterminesBestConnectionStringForAzureServiceBus().Determine(options.ReplyToAddress);
                        }

                        if (options.TimeToBeReceived.HasValue && options.TimeToBeReceived < TimeSpan.MaxValue)
                        {
                            brokeredMessage.TimeToLive = options.TimeToBeReceived.Value;
                        }

                        if (brokeredMessage.Size > 256*1024)
                        {
                            throw new MessageTooLargeException(string.Format("The message with id {0} is larger that the maximum message size allowed by Azure ServiceBus, consider using the databus instead", message.Id));
                        }

                        sender.Send(brokeredMessage);
                        sent = true;
                    }
                }
                catch (MessagingEntityNotFoundException)
                {
                    throw new QueueNotFoundException
                    {
                        Queue = address
                    };
                }
                    // todo: outbox
                catch (MessagingEntityDisabledException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));
                }
                    // back off when we're being throttled
                catch (ServerBusyException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));
                }
                    // connection lost
                catch (MessagingCommunicationException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));
                }
                    // took to long, maybe we lost connection
                catch (TimeoutException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));
                }
            }
        }


        DateTime? DelayIfNeeded(SendOptions options, bool expectDelay)
        {
            DateTime? deliverAt = null;

            if (options.DelayDeliveryWith.HasValue)
            {
                deliverAt = DateTime.UtcNow + options.DelayDeliveryWith.Value;
            }
            else
            {
                if (options.DeliverAt.HasValue)
                {
                    deliverAt = options.DeliverAt.Value;
                }
                else if (expectDelay)
                {
                    throw new ArgumentException("A delivery time needs to be specified for Deferred messages");
                }

            }

            return deliverAt;
        }

    }
}