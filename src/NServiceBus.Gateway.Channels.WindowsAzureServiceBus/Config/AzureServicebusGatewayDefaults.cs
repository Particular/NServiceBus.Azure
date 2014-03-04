namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus.Config
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureServicebusGatewayDefaults
    {
        /// <summary>
        /// 
        /// </summary>
        public const string DefaultIssuerName = "owner";

        /// <summary>
        /// 
        /// </summary>
        public const int DefaultLockDuration = 30000;

        /// <summary>
        /// 
        /// </summary>
        public const long DefaultMaxSizeInMegabytes = 1024;

        /// <summary>
        /// 
        /// </summary>
        public const bool DefaultRequiresDuplicateDetection = true;

        /// <summary>
        /// 
        /// </summary>
        public const bool DefaultRequiresSession = false;

        /// <summary>
        /// 
        /// </summary>
        public const long DefaultDefaultMessageTimeToLive = 92233720368547;

        /// <summary>
        /// 
        /// </summary>
        public const bool DefaultEnableDeadLetteringOnMessageExpiration = false;

        /// <summary>
        /// 
        /// </summary>
        public const bool EnableDeadLetteringOnFilterEvaluationExceptions = false;

        /// <summary>
        /// 
        /// </summary>
        public const int DefaultDuplicateDetectionHistoryTimeWindow = 600000;

        /// <summary>
        /// 
        /// </summary>
        public const int DefaultMaxDeliveryCount = 5;

        /// <summary>
        /// 
        /// </summary>
        public const bool DefaultEnableBatchedOperations = true;

        /// <summary>
        /// 
        /// </summary>
        public const int DefaultBackoffTimeInSeconds = 10;

        /// <summary>
        /// 
        /// </summary>
        public const int DefaultServerWaitTime = 300;

        /// <summary>
        /// 
        /// </summary>
        public const string DefaultConnectivityMode = "Tcp";

        /// <summary>
        /// 
        /// </summary>
        public const string DefaultConnectionString = "";

        /// <summary>
        /// 
        /// </summary>
        public const int DefaultBatchSize = 100;

        /// <summary>
        /// 
        /// </summary>
        public const bool DefaultEnablePartitioning = false;

        /// <summary>
        /// 
        /// </summary>
        public const int DefaultPrefetchCount = 2000;
    }
}