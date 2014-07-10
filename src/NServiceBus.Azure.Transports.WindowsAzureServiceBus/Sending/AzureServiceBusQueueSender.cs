namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    internal class AzureServiceBusQueueSender : ISendBrokeredMessages
    {
        const int DefaultBackoffTimeInSeconds = 10;

        public int MaxDeliveryCount { get; set; }

        public QueueClient QueueClient { get; set; }

        public async Task Send(BrokeredMessage brokeredMessage)
        {
            var numRetries = 0;
            var sent = false;

            while (!sent)
            {
                try
                {
                   await QueueClient.SendAsync(brokeredMessage);
                    
                   sent = true;
                }
                    // todo: outbox
                catch (MessagingEntityDisabledException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));
                }
                    // back off when we're being throttled
                catch (ServerBusyException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));
                }
                    // connection lost
                catch (MessagingCommunicationException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));
                }
                    // took to long, maybe we lost connection
                catch (TimeoutException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));
                }
            }
        }
    }
}