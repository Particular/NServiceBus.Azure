namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Logging;

    class AzureServiceBusQueueNotifier : INotifyReceivedBrokeredMessages
    {
        Action<BrokeredMessage> tryProcessMessage;
        bool cancelRequested;

        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusQueueNotifier));

        public Func<string, string, QueueClient> QueueClientFactory { get; set; }
        
        public int ServerWaitTime { get; set; }
        public int BatchSize { get; set; }
        public int BackoffTimeInSeconds { get; set; }

        public Type MessageType { get; set; }
        public string EntityName { get; set; }
        public IEnumerable<string> Namespaces { get; set; }

        Action<Exception> errorProcessingMessage;
        
        public void Start(Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage)
        {
            cancelRequested = false;

            this.tryProcessMessage = tryProcessMessage;
            this.errorProcessingMessage = errorProcessingMessage;

            foreach (var ns in Namespaces)
            {
                var queueClient = QueueClientFactory(EntityName, ns);

                SafeBeginReceive(new QueueClientMapping()
                {
                    QueueClient = queueClient,
                    QueueName = EntityName,
                    Namespace = ns
                });
            }
        }

        public void Stop()
        {
            cancelRequested = true;
        }

        void OnMessage(IAsyncResult ar)
        {
            var map = (QueueClientMapping) ar.AsyncState;
            try
            {
                if (!map.QueueClient.IsClosed)
                {
                    var receivedMessages = map.QueueClient.EndReceiveBatch(ar);

                    if (cancelRequested) return;

                    foreach (var receivedMessage in receivedMessages)
                    {
                        tryProcessMessage(receivedMessage);
                    }
                }
                else
                {
                    map.QueueClient = QueueClientFactory(map.QueueName, map.Namespace);
                }
            }
            catch (TimeoutException ex)
            {
                // time's up, just continue and retry
                logger.Warn(string.Format("Timeout Exception occured on queue {0}", map.QueueClient.Path), ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Fatal(string.Format("Unauthorized Access Exception occured on queue {0}", map.QueueClient.Path), ex);

                errorProcessingMessage(ex);
            }
            catch (MessagingException ex)
            {
                if (cancelRequested)
                {
                    return;
                }

                if (!ex.IsTransient && !RetriableReceiveExceptionHandling.IsRetryable(ex))
                {
                    logger.Fatal(string.Format("{1} {2} occured on queue {0}", map.QueueClient.Path, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);

                    errorProcessingMessage(ex);
                }
                else
                {
                    logger.Warn(string.Format("{1} {2} occured on queue {0}", map.QueueClient.Path, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);
                }
                
                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            catch (OperationCanceledException ex)
            {
                logger.Fatal(string.Format("Operation cancelled exception occured on receive for queue {0}, faulting this notifier", map.QueueClient.Path), ex);
                
                map.QueueClient = QueueClientFactory(map.QueueName, map.Namespace);
            }
            finally
            {
                SafeBeginReceive(map);
            }
        }

        void SafeBeginReceive(QueueClientMapping map)
        {
            if (!cancelRequested)
            {
                try
                {
                    map.QueueClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, map.QueueClient);
                }
                catch (OperationCanceledException ex)
                {
                    logger.Fatal(string.Format("Operation cancelled exception occured on receive for queue {0}, faulting this notifier", map.QueueName), ex);

                    map.QueueClient = QueueClientFactory(map.QueueName, map.Namespace);
                }
            }
        }

        class QueueClientMapping
        {
            public QueueClient QueueClient { get; set; }
            public string QueueName { get; set; }
            public string Namespace { get; set; }
        }
    }


}