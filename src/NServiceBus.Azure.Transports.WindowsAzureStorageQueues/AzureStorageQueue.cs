namespace NServiceBus
{
    using Azure.Transports.WindowsAzureStorageQueues;
    using Config;
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
        public override void Configure(ConfigurationBuilder config)
        {
            config.EnableFeature<AzureStorageQueueTransport>();
            config.EnableFeature<TimeoutManagerBasedDeferral>();

            config.Settings.EnableFeatureByDefault<MessageDrivenSubscriptions>();
            config.Settings.EnableFeatureByDefault<StorageDrivenPublishing>();
            config.Settings.EnableFeatureByDefault<TimeoutManager>();

            config.Settings.SetDefault("SelectedSerializer", typeof(Json));

            config.Settings.SetDefault("ScaleOut.UseSingleBrokerQueue", true); // default to one queue for all instances

            var configSection = config.Settings.GetConfigSection<AzureQueueConfig>();

            if (configSection == null)
                return;

            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.PurgeOnStartup, configSection.PurgeOnStartup);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.MaximumWaitTimeWhenIdle, configSection.MaximumWaitTimeWhenIdle);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.MessageInvisibleTime, configSection.MessageInvisibleTime);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.PeekInterval, configSection.PeekInterval);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.BatchSize, configSection.BatchSize);
        }
    }
}