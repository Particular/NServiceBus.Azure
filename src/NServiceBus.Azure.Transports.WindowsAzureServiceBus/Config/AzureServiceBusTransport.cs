namespace NServiceBus.Features
{
    using System;
    using System.Transactions;
    using Azure.Transports.WindowsAzureServiceBus;
    using Config;
    using Microsoft.ServiceBus;
    using Transports;

    internal class AzureServiceBusTransport : ConfigureTransport<AzureServiceBus>
    {
        protected override void InternalConfigure(Configure config)
        {
            config.Settings.SetDefault("SelectedSerializer", typeof(Json));

            var configSection = config.Settings.GetConfigSection<AzureServiceBusQueueConfig>();
            var serverWaitTime = AzureServicebusDefaults.DefaultServerWaitTime;

            if (configSection != null)
            {
                config.Settings.SetDefault("ScaleOut.UseSingleBrokerQueue", !configSection.QueuePerInstance);
                
                serverWaitTime = configSection.ServerWaitTime;
            }
            else
            {
                config.Settings.SetDefault("ScaleOut.UseSingleBrokerQueue", true); // default to one queue for all instances
            }
            
            var queuename = AzureServiceBusQueueNamingConvention.Apply(config.Settings.EndpointName());

            Address.InitializeLocalAddress(queuename);

            // make sure the transaction stays open a little longer than the long poll.
            config.Transactions( s => s.Advanced(settings => settings.DefaultTimeout(TimeSpan.FromSeconds(serverWaitTime * 1.1)).IsolationLevel(IsolationLevel.Serializable)));
            
            config.Features( f => f.Enable<AzureServiceBusTransport>());
            
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<AzureServiceBusQueueConfig>();
            if (configSection == null)
            {
                //hack: just to get the defaults, we should refactor this to support specifying the values on the NServiceBus/Transport connection string as well
                configSection = new AzureServiceBusQueueConfig();
            }

            var transportConfig = context.Settings.GetConfigSection<TransportConfig>() ?? new TransportConfig();

            ServiceBusEnvironment.SystemConnectivity.Mode = (ConnectivityMode)Enum.Parse(typeof(ConnectivityMode), configSection.ConnectivityMode);

            var connectionString = new DeterminesBestConnectionStringForAzureServiceBus().Determine(context.Settings);
            Address.OverrideDefaultMachine(connectionString);

            new ContainerConfiguration().Configure(context, configSection, transportConfig);
        }

        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "Endpoint=sb://{yournamespace}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={yourkey}"; }
        }

        
    }
}