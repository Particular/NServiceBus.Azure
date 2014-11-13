namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;

    class AzureServiceBusQueueSender : ISendBrokeredMessages
    {
        const int DefaultBackoffTimeInSeconds = 10;

        public int MaxDeliveryCount { get; set; }

        public QueueClient QueueClient { get; set; }

        public void Send(BrokeredMessage brokeredMessage)
        {
            var toSend = brokeredMessage;
            var numRetries = 0;
            var sent = false;

            while (!sent)
            {
                try
                {
                   QueueClient.Send(brokeredMessage);
                    
                   sent = true;
                }
                    // todo: outbox
                catch (MessagingEntityDisabledException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));

                    toSend = toSend.CloneWithMessageId();
                }
                    // back off when we're being throttled
                catch (ServerBusyException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));

                    toSend = toSend.CloneWithMessageId();
                }
                    // connection lost
                catch (MessagingCommunicationException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));

                    toSend = toSend.CloneWithMessageId();
                }
                    // took to long, maybe we lost connection
                catch (TimeoutException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries*DefaultBackoffTimeInSeconds));

                    toSend = toSend.CloneWithMessageId();
                }
            }
        }
    }
}