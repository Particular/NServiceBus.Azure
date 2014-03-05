using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System.IO;
    using Unicast.Queuing;
    using Unicast.Transport;

    /// <summary>
    /// 
    /// </summary>
    internal class AzureServiceBusGatewayQueueSender : ISendGatewayMessages
    {
        private readonly Dictionary<string, QueueClient> senders = new Dictionary<string, QueueClient>();
        
        private static readonly object SenderLock = new Object();

        public int MaxDeliveryCount { get; set; }

        public int BackoffTimeInSeconds { get; set; }

        ICreateMessagingFactories createMessagingFactories;

        public AzureServiceBusGatewayQueueSender(ICreateMessagingFactories createMessagingFactories)
        {
            this.createMessagingFactories = createMessagingFactories;
        }

        public void Send(Stream message, IDictionary<string, string> headers, string destination)
        {
            var @namespace = TransportConnectionString.GetConnectionStringOrNull("NServiceBus/Gateway/" + destination);

            Send(message, headers, new Address(destination, @namespace));
        }

        private void Send(Stream message, IDictionary<string, string> headers, Address address)
        {
            using (var tx = new TransactionScope(TransactionScopeOption.Suppress))
            {
                var destination = address.Queue;
                var @namespace = address.Machine;

                QueueClient sender;
                if (!senders.TryGetValue(destination, out sender))
                {
                    lock (SenderLock)
                    {
                        if (!senders.TryGetValue(destination, out sender))
                        {
                            var factory = createMessagingFactories.Create(@namespace);
                            sender = factory.CreateQueueClient(destination);
                            senders[destination] = sender;
                        }
                    }
                }

                var brokeredMessage = new BrokeredMessage(message, false);
                foreach (var header in headers)
                {
                    brokeredMessage.Properties[header.Key] = header.Value;
                }


                // if (!SettingsHolder.Get<bool>("Transactions.Enabled") || Transaction.Current == null)
                Send(brokeredMessage, sender, address);
                ///  else
                //   Transaction.Current.EnlistVolatile(new SendResourceManager(() => Send(brokeredMessage, sender, address)), EnlistmentOptions.None);
                tx.Complete();
            }
        }

        void Send(BrokeredMessage brokeredMessage, QueueClient sender, Address address)
        {
            var numRetries = 0;
            var sent = false;

            while (!sent)
            {
                try
                {
                   // using (brokeredMessage)
                    {
                        //brokeredMessage.CorrelationId = message.CorrelationId;
                        //if (message.TimeToBeReceived < TimeSpan.MaxValue) brokeredMessage.TimeToLive = message.TimeToBeReceived;

                       
                        //brokeredMessage.Properties[Headers.MessageIntent] = message.MessageIntent.ToString();
                        //brokeredMessage.MessageId = message.Id;

                        //if (message.ReplyToAddress != null)
                        //{
                        //    brokeredMessage.ReplyTo = message.ReplyToAddress.Queue;
                        //    //new DeterminesBestConnectionStringForAzureServiceBus().Determine(message.ReplyToAddress);
                        //}

                        //if (message.TimeToBeReceived < TimeSpan.MaxValue)
                        //{
                        //    brokeredMessage.TimeToLive = message.TimeToBeReceived;
                        //}
                        sender.Send(brokeredMessage);
                        
                        sent = true;
                    }

                    
                }
                catch (MessagingEntityNotFoundException)
                {
                    throw new QueueNotFoundException { Queue = address };
                }
                // todo: outbox
                catch (MessagingEntityDisabledException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * BackoffTimeInSeconds));
                }
                // back off when we're being throttled
                catch (ServerBusyException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * BackoffTimeInSeconds));
                }
                // connection lost
                catch (MessagingCommunicationException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * BackoffTimeInSeconds));
                }
                // took to long, maybe we lost connection
                catch (TimeoutException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * BackoffTimeInSeconds));
                }
            }
        }

      
   }
}