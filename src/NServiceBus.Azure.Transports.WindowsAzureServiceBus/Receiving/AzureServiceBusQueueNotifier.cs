using System;
using System.Threading;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusQueueNotifier : INotifyReceivedMessages
    {
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
                if (cancelRequested) return;

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            catch (ServerBusyException)
            {
                if (cancelRequested) return;

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            catch (MessagingCommunicationException)
            {
                if (cancelRequested) return;

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            catch (TimeoutException)
            {
                // time's up, just continue and retry
            }

            _queueClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
        }
    }
}