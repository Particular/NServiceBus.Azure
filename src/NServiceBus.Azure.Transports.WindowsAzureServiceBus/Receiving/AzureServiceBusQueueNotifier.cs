namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
<<<<<<< HEAD
    using Logging;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusQueueNotifier : INotifyReceivedMessages
    {
        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusQueueNotifier));

        private QueueClient _queueClient;
        private Action<BrokeredMessage> _tryProcessMessage;
        private Action<Exception> errorProcessingMessage;
        private bool cancelRequested;
=======
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Logging;

    class AzureServiceBusQueueNotifier : INotifyReceivedBrokeredMessages
    {
        Action<BrokeredMessage> tryProcessMessage;
        bool cancelRequested;

        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusQueueNotifier));
>>>>>>> release-6.0.0
        
        public QueueClient QueueClient { get; set; }

        public int ServerWaitTime { get; set; }
        public int BatchSize { get; set; }
        public int BackoffTimeInSeconds { get; set; }

        public Type MessageType { get; set; }
        public Address Address { get; set; }

<<<<<<< HEAD
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="tryProcessMessage"></param>
        /// <param name="errorProcessingMessage"></param>
        public void Start(Address address, Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage)
        {
            Address = address;
=======
        Action<Exception> errorProcessingMessage;
>>>>>>> release-6.0.0

        public void Start(Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage)
        {
            cancelRequested = false;

<<<<<<< HEAD
            this.errorProcessingMessage = errorProcessingMessage;
            _tryProcessMessage = tryProcessMessage;
=======
            this.tryProcessMessage = tryProcessMessage;
            this.errorProcessingMessage = errorProcessingMessage;
>>>>>>> release-6.0.0
            
            QueueClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
        }

        public void Stop()
        {
            cancelRequested = true;
        }

        void OnMessage(IAsyncResult ar)
        {
            try
            {
                var receivedMessages = QueueClient.EndReceiveBatch(ar);

                if (cancelRequested) return;

                foreach (var receivedMessage in receivedMessages)
                {
                    tryProcessMessage(receivedMessage);
                }
            }
            catch (TimeoutException ex)
            {
                // time's up, just continue and retry
<<<<<<< HEAD
                logger.Warn(string.Format("Timeout Exception occured on queue {0}", _queueClient.Path), ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Fatal(string.Format("Unauthorized Access Exception occured on queue {0}", _queueClient.Path), ex);
=======
                logger.Warn(string.Format("Timeout Exception occured on queue {0}", QueueClient.Path), ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Fatal(string.Format("Unauthorized Access Exception occured on queue {0}", QueueClient.Path), ex);
>>>>>>> release-6.0.0

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
<<<<<<< HEAD
                    logger.Fatal(string.Format("{1} {2} occured on queue {0}", _queueClient.Path, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);
=======
                    logger.Fatal(string.Format("{1} {2} occured on queue {0}", QueueClient.Path, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);
>>>>>>> release-6.0.0

                    errorProcessingMessage(ex);
                }
                else
                {
<<<<<<< HEAD
                    logger.Warn(string.Format("{1} {2} occured on queue {0}", _queueClient.Path, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);
=======
                    logger.Warn(string.Format("{1} {2} occured on queue {0}", QueueClient.Path, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);
>>>>>>> release-6.0.0
                }


                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            finally
            {
<<<<<<< HEAD
                _queueClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
=======
                QueueClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
>>>>>>> release-6.0.0
            }
        }
    }
}