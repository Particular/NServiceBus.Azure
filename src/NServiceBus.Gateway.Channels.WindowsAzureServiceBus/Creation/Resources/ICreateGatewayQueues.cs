namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System;

    public interface ICreateGatewayQueues
    {
        TimeSpan LockDuration { get; set; }
        long MaxSizeInMegabytes { get; set; }
        bool RequiresDuplicateDetection { get; set; }
        bool RequiresSession { get; set; }
        TimeSpan DefaultMessageTimeToLive { get; set; }
        bool EnableDeadLetteringOnMessageExpiration { get; set; }
        TimeSpan DuplicateDetectionHistoryTimeWindow { get; set; }
        int MaxDeliveryCount { get; set; }
        bool EnableBatchedOperations { get; set; }
        bool EnablePartitioning { get; set; }
        void Create(string queuename, string @namespace);
    }
}