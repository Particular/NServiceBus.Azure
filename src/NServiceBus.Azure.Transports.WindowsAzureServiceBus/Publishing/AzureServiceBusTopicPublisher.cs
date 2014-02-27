using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;


namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using NServiceBus.Transports;
    using Settings;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusTopicPublisher : IPublishMessages
    {
        public const int DefaultBackoffTimeInSeconds = 10;
        public int MaxDeliveryCount { get; set; }

        public ICreateTopicClients TopicClientCreator { get; set; }

        private readonly Dictionary<string, TopicClient> senders = new Dictionary<string, TopicClient>();
        private static readonly object SenderLock = new Object();
        
        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            var sender = GetTopicClientForDestination(Address.Local);

            if (sender == null) return false;

            if (!SettingsHolder.Get<bool>("Transactions.Enabled") || Transaction.Current == null)
                Send(message, sender);
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(() => Send(message, sender)), EnlistmentOptions.None);

            return true;
        }

        // todo, factor out... to bad IMessageSender is internal
        private void Send(TransportMessage message, TopicClient sender)
        {
            var numRetries = 0;
            var sent = false;

            while (!sent)
            {
                try
                {
                    SendTo(message, sender);
                    sent = true;
                }
                // todo, outbox
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
                // took to long, maybe we lost connection
                catch (TimeoutException)
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
            }
        }

        // todo, factor out... to bad IMessageSender is internal
        private void SendTo(TransportMessage message, TopicClient sender)
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
                
            }
        }

        // todo, factor out...
        private TopicClient GetTopicClientForDestination(Address destination)
        {
            var key = destination.ToString();
            TopicClient sender;
            if (!senders.TryGetValue(key, out sender))
            {
                lock (SenderLock)
                {
                    if (!senders.TryGetValue(key, out sender))
                    {
                        try
                        {
                            sender = TopicClientCreator.Create(destination);
                            senders[key] = sender;
                        }
                        catch (MessagingEntityNotFoundException)
                        {
                            // TopicNotFoundException?
                            //throw new QueueNotFoundException { Queue = Address.Parse(destination) };
                        }
                        catch (MessagingEntityAlreadyExistsException)
                        {
                            // is ok.
                        }
                    }
                }
            }
            return sender;
        }
    }
}