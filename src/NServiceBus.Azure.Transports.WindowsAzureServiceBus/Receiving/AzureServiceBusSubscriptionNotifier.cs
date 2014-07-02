using System;
using System.Threading;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusSubscriptionNotifier : INotifyReceivedBrokeredMessages
    {
        private SubscriptionClient subscriptionClient;
        private Action<BrokeredMessage> tryProcessMessage;
        private bool cancelRequested;

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

        /// <summary>
        /// 
        /// </summary>
        public ICreateSubscriptionClients SubscriptionClientCreator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Type MessageType { get; set; }

        public Address Address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tryProcessMessage"></param>
        public void Start(Action<BrokeredMessage> tryProcessMessage)
        {
            cancelRequested = false;

            this.tryProcessMessage = tryProcessMessage;

            subscriptionClient = SubscriptionClientCreator.Create(Address, MessageType);

            if (subscriptionClient != null) subscriptionClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
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
                var receivedMessages = subscriptionClient.EndReceiveBatch(ar);

                if (cancelRequested) return;

                foreach (var receivedMessage in receivedMessages)
                {
                    tryProcessMessage(receivedMessage);
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
            catch (MessagingException)
            {
                if (cancelRequested) return;

                Thread.Sleep(TimeSpan.FromSeconds(BackoffTimeInSeconds));
            }
            catch (TimeoutException)
            {
                // time's up, just continue and retry
            }

            subscriptionClient.BeginReceiveBatch(BatchSize, TimeSpan.FromSeconds(ServerWaitTime), OnMessage, null);
        }
    }
}