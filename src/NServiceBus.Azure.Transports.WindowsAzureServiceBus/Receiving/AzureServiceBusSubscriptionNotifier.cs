namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Logging;

    class AzureServiceBusSubscriptionNotifier : INotifyReceivedBrokeredMessages
    {
        Action<BrokeredMessage> tryProcessMessage;
        Action<Exception> errorProcessingMessage;
        bool cancelRequested;
        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusSubscriptionNotifier));

        public int ServerWaitTime { get; set; }
        public int BatchSize { get; set; }
        public int BackoffTimeInSeconds { get; set; }

        public Func<Type, string, string, SubscriptionClient> SubscriptionClientFactory { get; set; }

        public Type MessageType { get; set; }
        public string EntityName { get; set; }
        public IEnumerable<string> Namespaces { get; set; }

        public void Start(Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage)
        {
            cancelRequested = false;

            this.tryProcessMessage = tryProcessMessage;
            this.errorProcessingMessage = errorProcessingMessage;

            foreach (var ns in Namespaces)
            {
                var client = SubscriptionClientFactory(MessageType, EntityName, ns);

                var map = new SubscriptionClientMapping
                {
                    Namespace = ns,
                    TopicName = EntityName,
                    SubscriptionClient = client
                };

                SafeBeginReceive(map);
            }

            
        }

        public void Stop()
        {
            cancelRequested = true;
        }

        void OnMessage(IAsyncResult ar)
        {
            var map = (SubscriptionClientMapping) ar.AsyncState;
            try
            {
                if (!map.SubscriptionClient.IsClosed)
                {
                    var receivedMessages = map.SubscriptionClient.EndReceiveBatch(ar);

                    if (cancelRequested) return;

                    foreach (var receivedMessage in receivedMessages)
                    {
                        tryProcessMessage(receivedMessage);
                    }
                }
                else
                {
                    map.SubscriptionClient = SubscriptionClientFactory(MessageType, map.TopicName, map.Namespace);
                }
            }
            catch (TimeoutException ex)
            {

                logger.Warn(string.Format("Timeout communication exception occured on subscription {0}", map.SubscriptionClient.Name), ex);
                // time's up, just continue and retry
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Fatal(string.Format("Unauthorized Access Exception occured on subscription {0}", map.SubscriptionClient.Name), ex);

                // errorProcessingMessage(ex);
                // return
                // for now choosen to continue
            }
            catch (MessagingException ex)
            {
                if (cancelRequested)
                {
                    return;
                }

                if (!ex.IsTransient && !RetriableReceiveExceptionHandling.IsRetryable(ex))
                {
                    logger.Fatal(string.Format("{1} Messaging exception occured on subscription {0}", map.SubscriptionClient.Name, (ex.IsTransient ? "Transient" : "Non transient")), ex);

                    errorProcessingMessage(ex);
                }
                else
                {
                    logger.Warn(string.Format("{1} Messaging exception occured on subscription {0}", map.SubscriptionClient.Name, (ex.IsTransient ? "Transient" : "Non transient")), ex);
                }


                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            catch (OperationCanceledException ex)
            {
                logger.Fatal(string.Format("Operation cancelled exception occured on receive for subscription {0}, most likely due to a closed channel, faulting this notifier", map.SubscriptionClient.Name), ex);

                map.SubscriptionClient = SubscriptionClientFactory(MessageType, map.TopicName, map.Namespace);
            }
            finally
            {
                SafeBeginReceive(map);
            }
        }

        void SafeBeginReceive(SubscriptionClientMapping map)
        {
            if (!cancelRequested)
            {
                try
                {
                    map.SubscriptionClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, map);
                }
                catch (OperationCanceledException ex)
                {
                    logger.Fatal(string.Format("Operation cancelled exception occured on receive for subscription {0}, faulting this notifier", map.SubscriptionClient.Name), ex);

                    map.SubscriptionClient = SubscriptionClientFactory(MessageType, map.TopicName, map.Namespace);
                }
            }
        }

        class SubscriptionClientMapping
        {
            public SubscriptionClient SubscriptionClient { get; set; }
            public string TopicName { get; set; }
            public string Namespace { get; set; }
        }
    }
}