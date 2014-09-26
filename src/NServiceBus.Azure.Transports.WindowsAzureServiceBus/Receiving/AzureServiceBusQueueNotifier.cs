using System;
using System.Threading;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
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
        
        /// <summary>
        /// 
        /// </summary>
        public ICreateQueueClients QueueClientCreator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ServerWaitTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int BatchSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int BackoffTimeInSeconds { get; set; }

        public Address Address { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="tryProcessMessage"></param>
        /// <param name="errorProcessingMessage"></param>
        public void Start(Address address, Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage)
        {
            Address = address;

            cancelRequested = false;

            this.errorProcessingMessage = errorProcessingMessage;
            _tryProcessMessage = tryProcessMessage;
            
            _queueClient = QueueClientCreator.Create(address);

            _queueClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            cancelRequested = true;
        }

        private void OnMessage(IAsyncResult ar)
        {
            try
            {
                var receivedMessages = _queueClient.EndReceiveBatch(ar);

                if (cancelRequested) return;

                foreach (var receivedMessage in receivedMessages)
                {
                    _tryProcessMessage(receivedMessage);
                }
            }
            catch (TimeoutException ex)
            {
                // time's up, just continue and retry
                logger.Warn(string.Format("Timeout Exception occured on queue {0}", _queueClient.Path), ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Fatal(string.Format("Unauthorized Access Exception occured on queue {0}", _queueClient.Path), ex);

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
                    logger.Fatal(string.Format("{1} {2} occured on queue {0}", _queueClient.Path, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);

                    errorProcessingMessage(ex);
                }
                else
                {
                    logger.Warn(string.Format("{1} {2} occured on queue {0}", _queueClient.Path, (ex.IsTransient ? "Transient" : "Non transient"), ex.GetType().Name), ex);
                }


                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            finally
            {
                _queueClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
            }
        }
    }
}