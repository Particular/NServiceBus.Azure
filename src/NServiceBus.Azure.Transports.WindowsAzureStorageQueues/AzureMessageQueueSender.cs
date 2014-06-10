using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
using NServiceBus.Serialization;

namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using NServiceBus.Transports;
    using Unicast;
    using Unicast.Queuing;

    /// <summary>
    /// 
    /// </summary>
    public class AzureMessageQueueSender : ISendMessages
    {
        readonly Configure config;
        private readonly Dictionary<string, CloudQueueClient> destinationQueueClients = new Dictionary<string, CloudQueueClient>();
        private static readonly object SenderLock = new Object();

        /// <summary>
        /// Gets or sets the message serializer
        /// </summary>
        public IMessageSerializer MessageSerializer { get; set; }

        public CloudQueueClient Client { get; set; }

        public AzureMessageQueueSender(Configure config)
        {
            this.config = config;
        }

        public void Send(TransportMessage message, SendOptions options)
        {
           var address = options.Destination;

            var sendClient = GetClientForConnectionString(address.Machine) ?? Client;

            var sendQueue = sendClient.GetQueueReference(AzureMessageQueueUtils.GetQueueName(address));

            if (!sendQueue.Exists())
                throw new QueueNotFoundException { Queue = address };

            var timeToBeReceived = message.TimeToBeReceived < TimeSpan.MaxValue ? message.TimeToBeReceived : (TimeSpan?)null;

            var rawMessage = SerializeMessage(message);

            if (!config.Settings.Get<bool>("Transactions.Enabled") || Transaction.Current == null)
            {
                sendQueue.AddMessage(rawMessage, timeToBeReceived);
            }
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(sendQueue, rawMessage, timeToBeReceived), EnlistmentOptions.None);
        }

        private CloudQueueClient GetClientForConnectionString(string connectionString)
        {
            CloudQueueClient sendClient;

            var validation = new DeterminesBestConnectionStringForStorageQueues();
            if (!validation.IsPotentialStorageQueueConnectionString(connectionString))
            {
                connectionString = validation.Determine(config.Settings); 
            }

            if (!destinationQueueClients.TryGetValue(connectionString, out sendClient))
            {
                lock (SenderLock)
                {
                    if (!destinationQueueClients.TryGetValue(connectionString, out sendClient))
                    {
                        CloudStorageAccount account;

                        if (CloudStorageAccount.TryParse(connectionString, out account))
                        {
                            sendClient = account.CreateCloudQueueClient();
                        }

                        // sendClient could be null, this is intentional 
                        // so that it remembers a connectionstring was invald 
                        // and doesn't try to parse it again.

                        destinationQueueClients.Add(connectionString, sendClient);
                    }
                }
            }

            return sendClient;
        }

        private CloudQueueMessage SerializeMessage(TransportMessage message)
        {
            using (var stream = new MemoryStream())
            {
                var validation = new DeterminesBestConnectionStringForStorageQueues();
                var replyToAddress = validation.Determine(config.Settings,  message.ReplyToAddress ?? Address.Local );

                var toSend = new MessageWrapper
                    {
                        Id = message.Id,
                        Body = message.Body,
                        CorrelationId = message.CorrelationId,
                        Recoverable = message.Recoverable,
                        ReplyToAddress = replyToAddress,
                        TimeToBeReceived = message.TimeToBeReceived,
                        Headers = message.Headers,
                        MessageIntent = message.MessageIntent
                    };


                MessageSerializer.Serialize(new IMessage[] { toSend }, stream);
                return new CloudQueueMessage(stream.ToArray());
            }
        }
    }
}