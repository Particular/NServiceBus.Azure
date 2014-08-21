namespace NServiceBus
{
    using System;
    using System.Transactions;
    using Azure.Transports.WindowsAzureServiceBus;
    using Config;
    using Configuration.AdvanceExtensibility;
    using Features;
    using Transports;

    /// <summary>
    /// Transport definition for WindowsAzureServiceBus    
    /// </summary>
    public class AzureServiceBus : TransportDefinition
    {
        public AzureServiceBus()
        {
            HasNativePubSubSupport = true;
            HasSupportForCentralizedPubSub = false;
            HasSupportForDistributedTransactions = false;
        }

        protected override void Configure(ConfigurationBuilder config)
        {
            config.GetSettings().SetDefault("SelectedSerializer", typeof(Json));

            // make sure the transaction stays open a little longer than the long poll.
            config.Transactions().DefaultTimeout(TimeSpan.FromSeconds(AzureServicebusDefaults.DefaultServerWaitTime * 1.1)).IsolationLevel(IsolationLevel.Serializable);

            config.EnableFeature<AzureServiceBusTransport>();
        }
    }
}