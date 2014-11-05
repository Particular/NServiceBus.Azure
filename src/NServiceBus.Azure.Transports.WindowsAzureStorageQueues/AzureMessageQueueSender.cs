namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Transactions;
    using NServiceBus.Logging;
    using NServiceBus.Serialization;
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
        Configure config;
        ICreateQueueClients createQueueClients;
        ILog logger = LogManager.GetLogger(typeof(AzureMessageQueueSender));

        static Dictionary<string, bool> rememberExistence = new Dictionary<string, bool>();

        static object ExistenceLock = new Object();

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
            timeToBeReceived = timeToBeReceived ?? message.TimeToBeReceived;

            if (timeToBeReceived.Value == TimeSpan.Zero)
            {
                var messageType = message.Headers[Headers.EnclosedMessageTypes].Split(',').First();
                logger.WarnFormat("TimeToBeReceived is set to zero for message of type '{0}'. Cannot send message.", messageType);
                return;
            }

            // user explicitly specified TimeToBeReceived that is not TimeSpan.MaxValue - fail
            if (timeToBeReceived.Value > CloudQueueMessage.MaxTimeToLive && timeToBeReceived != TimeSpan.MaxValue)
            {
                var messageType = message.Headers[Headers.EnclosedMessageTypes].Split(',').First();
                throw new InvalidOperationException(string.Format("TimeToBeReceived is set to more than 7 days (maximum for Azure Storage queue) for message type '{0}'.",
                    messageType));
            }

            // TimeToBeReceived was not specified on message - go for maximum set by SDK
            if (timeToBeReceived == TimeSpan.MaxValue)
            {
                timeToBeReceived = null;
            }

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
            if (!rememberExistence.ContainsKey(key))
            {
                lock (ExistenceLock)
                {
                    exists = sendQueue.Exists();
                    rememberExistence[key] = exists;
                }
            }
            else
            {
                exists = rememberExistence[key];
            }

            return exists;
        }

        CloudQueueMessage SerializeMessage(TransportMessage message, SendOptions options)
        {
            using (var stream = new MemoryStream())
            {
                var validation = new DeterminesBestConnectionStringForStorageQueues();
                var replyToAddress = validation.Determine(config.Settings, message.ReplyToAddress ?? options.ReplyToAddress ?? config.LocalAddress, config.TransportConnectionString());

                var toSend = new MessageWrapper
                    {
                        Id = message.Id,
                        Body = message.Body,
                        CorrelationId = message.CorrelationId ?? options.CorrelationId,
                        Recoverable = message.Recoverable,
                        ReplyToAddress = replyToAddress,
                        TimeToBeReceived = options.TimeToBeReceived.HasValue ? options.TimeToBeReceived.Value : message.TimeToBeReceived,
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
        ConcurrentDictionary<string, CloudQueueClient> destinationQueueClients = new ConcurrentDictionary<string, CloudQueueClient>();
        Configure config;

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
                    connectionString = validation.Determine(config.Settings, config.TransportConnectionString());
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