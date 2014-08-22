namespace NServiceBus
{
    using System.Configuration;
    using Azure.Transports.WindowsAzureStorageQueues;
    using Config;
    using Configuration.AdvanceExtensibility;
    using Features;
    using Transports;

    /// <summary>
    /// Transport definition for AzureStorageQueue
    /// </summary>
    public class AzureStorageQueue : TransportDefinition
    {
        public AzureStorageQueue()
        {
            HasSupportForDistributedTransactions = false;
        }

        /// <summary>
        /// Gives implementations access to the <see cref="ConfigurationBuilder"/> instance at configuration time.
        /// </summary>
        protected override void Configure(ConfigurationBuilder config)
        {
            config.EnableFeature<AzureStorageQueueTransport>();
            config.EnableFeature<TimeoutManagerBasedDeferral>();

            config.GetSettings().EnableFeatureByDefault<MessageDrivenSubscriptions>();
            config.GetSettings().EnableFeatureByDefault<StorageDrivenPublishing>();
            config.GetSettings().EnableFeatureByDefault<TimeoutManager>();

            config.GetSettings().SetDefault("SelectedSerializer", typeof(Json));

            config.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true); // default to one queue for all instances
            
        }
    }
}