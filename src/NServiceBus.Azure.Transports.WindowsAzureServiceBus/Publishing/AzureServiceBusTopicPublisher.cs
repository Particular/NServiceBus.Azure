using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;


namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Logging;
    using NServiceBus.Transports;
    using Settings;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusTopicPublisher : IPublishMessages
    {
        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusTopicPublisher));

        public const int DefaultBackoffTimeInSeconds = 10;
        public int MaxDeliveryCount { get; set; }

        public ICreateTopicClients TopicClientCreator { get; set; }

        private static readonly Dictionary<string, TopicClient> senders = new Dictionary<string, TopicClient>();
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
                    logger.Warn(string.Format("Topic {0} is disabled", sender.Path)); 

                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    logger.Warn("Will retry after backoff period");

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
                // back off when we're being throttled
                catch (ServerBusyException ex)
                {
                    logger.Warn(string.Format("Server busy exception occured on topic {0}", sender.Path), ex);

                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    logger.Warn("Will retry after backoff period");

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
                // took to long, maybe we lost connection
                catch (TimeoutException ex)
                {
                    logger.Warn(string.Format("Timeout exception occured on topic {0}", sender.Path), ex);

                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    logger.Warn("Will retry after backoff period");

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
                // connection lost
                catch (MessagingCommunicationException ex)
                {
                    logger.Warn(string.Format("Messaging Communication Exception occured on topic {0}", sender.Path), ex);

                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    logger.Warn("Will retry after backoff period");

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
                catch (MessagingException ex)
                {
                    logger.Warn(string.Format("{1} Messaging Exception occured on topic {0}", sender.Path, (ex.IsTransient ? "Transient": "Non transient")), ex);

                    numRetries++;

                    if (numRetries >= MaxDeliveryCount || !ex.IsTransient) throw;

                    logger.Warn("Will retry after backoff period");

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