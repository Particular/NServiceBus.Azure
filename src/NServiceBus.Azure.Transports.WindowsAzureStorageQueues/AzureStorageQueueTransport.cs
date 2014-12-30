namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;
    using Features;
    using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
    using Transports;

    /// <summary>
    /// Transport definition for AzureStorageQueue
    /// </summary>
    public class AzureStorageQueueTransport : TransportDefinition
    {
        public AzureStorageQueueTransport()
        {
            HasSupportForDistributedTransactions = false;
        }

        /// <summary>
        /// Gives implementations access to the <see cref="T:NServiceBus.BusConfiguration"/> instance at configuration time.
        /// </summary>
        protected override void Configure(BusConfiguration config)
        {
            config.EnableFeature<AzureStorageQueueTransportConfiguration>();
            config.EnableFeature<TimeoutManagerBasedDeferral>();
            
            var settings = config.GetSettings();

            settings.EnableFeatureByDefault<MessageDrivenSubscriptions>();
            settings.EnableFeatureByDefault<StorageDrivenPublishing>();
            settings.EnableFeatureByDefault<TimeoutManager>();

            settings.SetDefault("SelectedSerializer", new JsonSerializer());

            settings.SetDefault("ScaleOut.UseSingleBrokerQueue", true); // default to one queue for all instances
            config.GetSettings().SetDefault("EndpointInstanceDiscriminator", QueueIndividualizer.Discriminator);
            
        }
    }
}