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

        public AzureServiceBusQueueCreator(ICreateNamespaceManagers createNamespaceManagers)
        {
            this.createNamespaceManagers = createNamespaceManagers;
        }

        public void Create(Address address)
        {
            var queueName = address.Queue;
            var path = "";
            var namespaceClient = createNamespaceManagers.Create(address.Machine);
            try
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

                path = description.Path;
                if (!namespaceClient.QueueExists(path))
                {
                    namespaceClient.CreateQueue(description);
                }
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the queue already exists or another node beat us to it, which is ok
            }
            catch (TimeoutException)
            {
                // there is a chance that the timeout occurs, but the queue is created still
                // check for this
                if (!namespaceClient.QueueExists(path))
                    throw;
            }
        }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            Create(address);
        }
    }

}