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
        public void Start(Address address, Action<BrokeredMessage> tryProcessMessage)
        {
            Address = address;

            cancelRequested = false;

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
            catch (MessagingEntityDisabledException)
            {
                logger.Warn(string.Format("Queue {0} is disable", _queueClient.Path));

                if (cancelRequested) return;

                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            catch (ServerBusyException ex)
            {
                logger.Warn(string.Format("Server Busy Exception occured on queue {0}", _queueClient.Path), ex);

                if (cancelRequested) return;

                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            catch (MessagingCommunicationException ex)
            {
                logger.Warn(string.Format("Messaging Communication Exception occured on queue {0}", _queueClient.Path), ex);

                if (cancelRequested) return;

                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            catch (TimeoutException ex)
            {
                // time's up, just continue and retry
                logger.Warn(string.Format("Timeout Exception occured on queue {0}", _queueClient.Path), ex);
            }
            catch (MessagingException ex)
            {
                logger.Warn(string.Format("{1} Messaging Exception occured on queue {0}", _queueClient.Path, (ex.IsTransient ? "Transient" : "Non transient")), ex);

                if (cancelRequested) return;

                logger.Warn("Will retry after backoff period");

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }

            _queueClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
        }
    }
}