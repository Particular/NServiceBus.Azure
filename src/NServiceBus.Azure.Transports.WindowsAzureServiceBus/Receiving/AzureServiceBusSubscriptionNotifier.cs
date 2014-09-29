namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
<<<<<<< HEAD
    using Logging;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusSubscriptionNotifier : INotifyReceivedMessages
    {
        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusSubscriptionNotifier));

        private SubscriptionClient subscriptionClient;
        private Action<BrokeredMessage> tryProcessMessage;
        private bool cancelRequested;
        Action<Exception> errorProcessingMessage;
=======
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Logging;

    class AzureServiceBusSubscriptionNotifier : INotifyReceivedBrokeredMessages
    {
        Action<BrokeredMessage> tryProcessMessage;
        Action<Exception> errorProcessingMessage;
        bool cancelRequested;
        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusSubscriptionNotifier));
>>>>>>> release-6.0.0

        public int ServerWaitTime { get; set; }
        public int BatchSize { get; set; }
        public int BackoffTimeInSeconds { get; set; }
        public SubscriptionClient SubscriptionClient { get; set; }
        public Type MessageType { get; set; }
        public Address Address { get; set; }

<<<<<<< HEAD
        /// <summary>
        /// 
        /// </summary>
        public ICreateSubscriptionClients SubscriptionClientCreator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Type EventType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="tryProcessMessage"></param>
        /// <param name="errorProcessingMessage"></param>
        public void Start(Address address, Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage)
=======
        public void Start(Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage)
>>>>>>> release-6.0.0
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
<<<<<<< HEAD
                logger.Warn(string.Format("Timeout communication exception occured on subscription {0}", subscriptionClient.Name), ex);
=======
                logger.Warn(string.Format("Timeout communication exception occured on subscription {0}", SubscriptionClient.Name), ex);
>>>>>>> release-6.0.0
                // time's up, just continue and retry
            }
            catch (UnauthorizedAccessException ex)
            {
<<<<<<< HEAD
                logger.Fatal(string.Format("Unauthorized Access Exception occured on subscription {0}", subscriptionClient.Name), ex);
=======
                logger.Fatal(string.Format("Unauthorized Access Exception occured on subscription {0}", SubscriptionClient.Name), ex);
>>>>>>> release-6.0.0

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
<<<<<<< HEAD
                    logger.Fatal(string.Format("{1} Messaging exception occured on subscription {0}", subscriptionClient.Name, (ex.IsTransient ? "Transient" : "Non transient")), ex);
=======
                    logger.Fatal(string.Format("{1} Messaging exception occured on subscription {0}", SubscriptionClient.Name, (ex.IsTransient ? "Transient" : "Non transient")), ex);
>>>>>>> release-6.0.0

                    errorProcessingMessage(ex);
                }
                else
                {
<<<<<<< HEAD
                    logger.Warn(string.Format("{1} Messaging exception occured on subscription {0}", subscriptionClient.Name, (ex.IsTransient ? "Transient" : "Non transient")), ex);
=======
                    logger.Warn(string.Format("{1} Messaging exception occured on subscription {0}", SubscriptionClient.Name, (ex.IsTransient ? "Transient" : "Non transient")), ex);
>>>>>>> release-6.0.0
                }


                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            finally
            {
<<<<<<< HEAD
                subscriptionClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
=======
                SubscriptionClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
>>>>>>> release-6.0.0
            }
        }
    }
}