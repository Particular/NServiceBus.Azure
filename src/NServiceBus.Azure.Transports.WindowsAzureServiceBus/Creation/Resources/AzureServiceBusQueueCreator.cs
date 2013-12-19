namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Transports;

    public class AzureServiceBusQueueCreator : ICreateQueues
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
        public bool EnablePartitioning { get; set; }

        readonly ICreateNamespaceManagers createNamespaceManagers;

        public AzureServiceBusQueueCreator() : this(new CreatesNamespaceManagers())
        {
        }

        public AzureServiceBusQueueCreator(ICreateNamespaceManagers createNamespaceManagers)
        {
            this.createNamespaceManagers = createNamespaceManagers;
        }

        public void Create(Address address)
        {
            var queueName = address.Queue;
            try
            {
                var namespaceClient = createNamespaceManagers.Create(address.Machine);
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
                        EnableBatchedOperations = EnableBatchedOperations,
                        EnablePartitioning = EnablePartitioning
                    };

                    namespaceClient.CreateQueue(description);
                }
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the queue already exists or another node beat us to it, which is ok
            }
        }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            Create(address);
        }
    }

}