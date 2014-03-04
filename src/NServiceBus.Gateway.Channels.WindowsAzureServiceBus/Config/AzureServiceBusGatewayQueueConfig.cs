namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus.Config
{
    using System.Configuration;

    public class AzureServiceBusGatewayQueueConfig : ConfigurationSection
    {
        [ConfigurationProperty("IssuerKey", IsRequired = false)]
        public string IssuerKey
        {
            get
            {
                return (string)this["IssuerKey"];
            }
            set
            {
                this["IssuerKey"] = value;
            }
        }

        [ConfigurationProperty("ServiceNamespace", IsRequired = false)]
        public string ServiceNamespace
        {
            get
            {
                return (string)this["ServiceNamespace"];
            }
            set
            {
                this["ServiceNamespace"] = value;
            }
        }

        [ConfigurationProperty("IssuerName", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultIssuerName)]
        public string IssuerName
        {
            get
            {
                return (string)this["IssuerName"];
            }
            set
            {
                this["IssuerName"] = value;
            }
        }

        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultConnectionString)]
        public string ConnectionString
        {
            get
            {
                return (string)this["ConnectionString"];
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }


        [ConfigurationProperty("LockDuration", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultLockDuration)]
        public int LockDuration
        {
            get
            {
                return (int)this["LockDuration"];
            }
            set
            {
                this["LockDuration"] = value;
            }
        }

        [ConfigurationProperty("MaxSizeInMegabytes", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultMaxSizeInMegabytes)]
        public long MaxSizeInMegabytes
        {
            get
            {
                return (long)this["MaxSizeInMegabytes"];
            }
            set
            {
                this["MaxSizeInMegabytes"] = value;
            }
        }

        [ConfigurationProperty("RequiresDuplicateDetection", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultRequiresDuplicateDetection)]
        public bool RequiresDuplicateDetection
        {
            get
            {
                return (bool)this["RequiresDuplicateDetection"];
            }
            set
            {
                this["RequiresDuplicateDetection"] = value;
            }
        }

        [ConfigurationProperty("RequiresSession", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultRequiresSession)]
        public bool RequiresSession
        {
            get
            {
                return (bool)this["RequiresSession"];
            }
            set
            {
                this["RequiresSession"] = value;
            }
        }

        [ConfigurationProperty("DefaultMessageTimeToLive", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultDefaultMessageTimeToLive)]
        public long DefaultMessageTimeToLive
        {
            get
            {
                return (long)this["DefaultMessageTimeToLive"];
            }
            set
            {
                this["DefaultMessageTimeToLive"] = value;
            }
        }

        [ConfigurationProperty("EnableDeadLetteringOnMessageExpiration", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultEnableDeadLetteringOnMessageExpiration)]
        public bool EnableDeadLetteringOnMessageExpiration
        {
            get
            {
                return (bool)this["EnableDeadLetteringOnMessageExpiration"];
            }
            set
            {
                this["EnableDeadLetteringOnMessageExpiration"] = value;
            }
        }

        [ConfigurationProperty("EnableDeadLetteringOnFilterEvaluationExceptions", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.EnableDeadLetteringOnFilterEvaluationExceptions)]
        public bool EnableDeadLetteringOnFilterEvaluationExceptions
        {
            get
            {
                return (bool)this["EnableDeadLetteringOnFilterEvaluationExceptions"];
            }
            set
            {
                this["EnableDeadLetteringOnFilterEvaluationExceptions"] = value;
            }
        }

        [ConfigurationProperty("DuplicateDetectionHistoryTimeWindow", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultDuplicateDetectionHistoryTimeWindow)]
        public int DuplicateDetectionHistoryTimeWindow
        {
            get
            {
                return (int)this["DuplicateDetectionHistoryTimeWindow"];
            }
            set
            {
                this["DuplicateDetectionHistoryTimeWindow"] = value;
            }
        }

        [ConfigurationProperty("MaxDeliveryCount", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultMaxDeliveryCount)]
        public int MaxDeliveryCount
        {
            get
            {
                return (int)this["MaxDeliveryCount"];
            }
            set
            {
                this["MaxDeliveryCount"] = value;
            }
        }

        [ConfigurationProperty("EnableBatchedOperations", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultEnableBatchedOperations)]
        public bool EnableBatchedOperations
        {
            get
            {
                return (bool)this["EnableBatchedOperations"];
            }
            set
            {
                this["EnableBatchedOperations"] = value;
            }
        }

        [ConfigurationProperty("ServerWaitTime", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultServerWaitTime)]
        public int ServerWaitTime
        {
            get
            {
                return (int)this["ServerWaitTime"];
            }
            set
            {
                this["ServerWaitTime"] = value;
            }
        }

        [ConfigurationProperty("ConnectivityMode", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultConnectivityMode)]
        public string ConnectivityMode
        {
            get
            {
                return (string)this["ConnectivityMode"];
            }
            set
            {
                this["ConnectivityMode"] = value;
            }
        }

        [ConfigurationProperty("BatchSize", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultBatchSize)]
        public int BatchSize
        {
            get
            {
                return (int)this["BatchSize"];
            }
            set
            {
                this["BatchSize"] = value;
            }
        }

        [ConfigurationProperty("BackoffTimeInSeconds", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultBackoffTimeInSeconds)]
        public int BackoffTimeInSeconds
        {
            get
            {
                return (int)this["BackoffTimeInSeconds"];
            }
            set
            {
                this["BackoffTimeInSeconds"] = value;
            }
        }

        [ConfigurationProperty("EnablePartitioning", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultEnablePartitioning)]
        public bool EnablePartitioning
        {
            get
            {
                return (bool)this["EnablePartitioning"];
            }
            set
            {
                this["EnablePartitioning"] = value;
            }
        }

        [ConfigurationProperty("PrefetchCount", IsRequired = false, DefaultValue = AzureServicebusGatewayDefaults.DefaultPrefetchCount)]
        public int PrefetchCount
        {
            get
            {
                return (int)this["PrefetchCount"];
            }
            set
            {
                this["PrefetchCount"] = value;
            }
        }
        
    }
}