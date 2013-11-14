using System;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    public class AzureServicebusQueueClientCreator : ICreateQueueClients
    {
        public TimeSpan LockDuration { get; set; }
        public long MaxSizeInMegabytes { get; set; }
        public bool RequiresDuplicateDetection { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public TimeSpan DuplicateDetectionHistoryTimeWindow { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }

        public QueueClient Create(Address address)
        {
            var queueName = CreateQueue(address);


            var factory = new CreatesMessagingFactories().Create(address.Machine);
            var client = factory.CreateQueueClient(queueName, ReceiveMode.PeekLock);
            client.PrefetchCount = 100; // todo make configurable
            return client;
        }

        public string CreateQueue(Address address)
        {
            var queueName = address.Queue;
            try
            {
                var namespaceClient = new CreatesNamespaceManagers().Create(address.Machine);
                if (!namespaceClient.QueueExists(queueName))
                {
                    var description = new QueueDescription(queueName)
                    {
                        LockDuration = LockDuration,
                        MaxSizeInMegabytes = MaxSizeInMegabytes,
                        RequiresDuplicateDetection = RequiresDuplicateDetection,
                        RequiresSession = RequiresSession,
                        DefaultMessageTimeToLive = DefaultMessageTimeToLive,
                        EnableDeadLetteringOnMessageExpiration = EnableDeadLetteringOnMessageExpiration,
                        DuplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow,
                        MaxDeliveryCount = MaxDeliveryCount,
                        EnableBatchedOperations = EnableBatchedOperations
                    };

                    namespaceClient.CreateQueue(description);
                }
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the queue already exists or another node beat us to it, which is ok
            }
            return queueName;
        }
    }
}