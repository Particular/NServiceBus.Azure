namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;
    using Logging;

    class AzureServiceBusSubscriptionNotifier : INotifyReceivedBrokeredMessages
    {
        Action<BrokeredMessage> tryProcessMessage;
        Action<Exception> errorProcessingMessage;
        bool cancelRequested;
        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusSubscriptionNotifier));

        public int ServerWaitTime { get; set; }
        public int BatchSize { get; set; }
        public int BackoffTimeInSeconds { get; set; }
        public SubscriptionClient SubscriptionClient { get; set; }
        public Type MessageType { get; set; }
        public Address Address { get; set; }

        public void Start(Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage)
        {
            cancelRequested = false;

            this.tryProcessMessage = tryProcessMessage;
            this.errorProcessingMessage = errorProcessingMessage;

            SubscriptionClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
        }

        public void Stop()
        {
            cancelRequested = true;
        }

        void OnMessage(IAsyncResult ar)
        {
            try
            {
                var receivedMessages = SubscriptionClient.EndReceiveBatch(ar);

                if (cancelRequested) return;

                foreach (var receivedMessage in receivedMessages)
                {
                    tryProcessMessage(receivedMessage);
                }
            }
            catch (TimeoutException ex)
            {

                logger.Warn(string.Format("Timeout communication exception occured on subscription {0}", SubscriptionClient.Name), ex);
                // time's up, just continue and retry
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Fatal(string.Format("Unauthorized Access Exception occured on subscription {0}", SubscriptionClient.Name), ex);

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
                    logger.Fatal(string.Format("{1} Messaging exception occured on subscription {0}", SubscriptionClient.Name, (ex.IsTransient ? "Transient" : "Non transient")), ex);

                    errorProcessingMessage(ex);
                }
                else
                {
                    logger.Warn(string.Format("{1} Messaging exception occured on subscription {0}", SubscriptionClient.Name, (ex.IsTransient ? "Transient" : "Non transient")), ex);
                }


                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            finally
            {
                SubscriptionClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
            }
        }
    }
}