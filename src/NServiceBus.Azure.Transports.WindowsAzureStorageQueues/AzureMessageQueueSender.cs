using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
using NServiceBus.Serialization;

namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System.Collections.Concurrent;
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
        readonly ICreateQueueClients createQueueClients;


        private static readonly Dictionary<string, bool> rememberExistance = new Dictionary<string, bool>();
        
        private static readonly object ExistanceLock = new Object();

        /// <summary>
        /// Gets or sets the message serializer
        /// </summary>
        public IMessageSerializer MessageSerializer { get; set; }

        public AzureMessageQueueSender(Configure config, ICreateQueueClients createQueueClients)
        {
            this.config = config;
            this.createQueueClients = createQueueClients;
        }

        public void Send(TransportMessage message, SendOptions options)
        {
            var address = options.Destination;

            var sendClient = createQueueClients.Create(address.Machine);

            var sendQueue = sendClient.GetQueueReference(AzureMessageQueueUtils.GetQueueName(address));

            if (!Exists(sendQueue))
                throw new QueueNotFoundException { Queue = address };

            var timeToBeReceived = options.TimeToBeReceived.HasValue && options.TimeToBeReceived < TimeSpan.MaxValue ? options.TimeToBeReceived : null;

            var rawMessage = SerializeMessage(message, options);

            if (!config.Settings.Get<bool>("Transactions.Enabled") || Transaction.Current == null)
            {
                sendQueue.AddMessage(rawMessage, timeToBeReceived);
                
            }
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(sendQueue, rawMessage, timeToBeReceived), EnlistmentOptions.None);
        }

        bool Exists(CloudQueue sendQueue)
        {
            var key = sendQueue.Uri.ToString();
            bool exists;
            if (!rememberExistance.ContainsKey(key))
            {
                lock (ExistanceLock)
                {
                    exists = sendQueue.Exists();
                    rememberExistance[key] = exists;
                }
            }
            else
            {
                 exists = rememberExistance[key];
            }

            return exists;
        }

        private CloudQueueMessage SerializeMessage(TransportMessage message, SendOptions options)
        {
            using (var stream = new MemoryStream())
            {
                var validation = new DeterminesBestConnectionStringForStorageQueues();
                var replyToAddress = validation.Determine(config.Settings, message.ReplyToAddress ?? options.ReplyToAddress ?? config.LocalAddress);

                var toSend = new MessageWrapper
                    {
                        Id = message.Id,
                        Body = message.Body,
                        CorrelationId = message.CorrelationId ?? options.CorrelationId,
                        Recoverable = message.Recoverable,
                        ReplyToAddress = replyToAddress,
                        TimeToBeReceived = options.TimeToBeReceived.HasValue ? options.TimeToBeReceived.Value : default(TimeSpan),
                        Headers = message.Headers,
                        MessageIntent = message.MessageIntent
                    };


                MessageSerializer.Serialize(toSend, stream);
                return new CloudQueueMessage(stream.ToArray());
            }
        }
    }

    public interface ICreateQueueClients
    {
        CloudQueueClient Create(string connectionString);
    }

    public class CreateQueueClients : ICreateQueueClients
    {
        private readonly ConcurrentDictionary<string, CloudQueueClient> destinationQueueClients = new ConcurrentDictionary<string, CloudQueueClient>();
        private readonly Configure config;

        public CreateQueueClients(Configure config)
        {
            this.config = config;
        }

        public CloudQueueClient Create(string connectionString)
        {
            return destinationQueueClients.GetOrAdd(connectionString, s =>
            {
                var validation = new DeterminesBestConnectionStringForStorageQueues();
                if (!validation.IsPotentialStorageQueueConnectionString(connectionString))
                {
                    connectionString = validation.Determine(config.Settings);
                }

                CloudQueueClient sendClient = null;
                CloudStorageAccount account;

                if (CloudStorageAccount.TryParse(connectionString, out account))
                {
                    sendClient = account.CreateCloudQueueClient();
                }

                return sendClient;
            });
        }
    }
}