namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
<<<<<<< HEAD
    using Logging;
    using NServiceBus.Transports;
    using Settings;
=======
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;
>>>>>>> release-6.0.0

    class AzureServiceBusTopicPublisher : IPublishBrokeredMessages
    {
<<<<<<< HEAD
        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusTopicPublisher));
=======
        public TopicClient TopicClient { get; set; }
>>>>>>> release-6.0.0

        public const int DefaultBackoffTimeInSeconds = 10;
        public int MaxDeliveryCount { get; set; }

<<<<<<< HEAD
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
=======
        public void Publish(BrokeredMessage brokeredMessage)
>>>>>>> release-6.0.0
        {
            var numRetries = 0;
            var sent = false;

            while (!sent)
            {
                try
                {
                    TopicClient.Send(brokeredMessage);
                   
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
    }
}