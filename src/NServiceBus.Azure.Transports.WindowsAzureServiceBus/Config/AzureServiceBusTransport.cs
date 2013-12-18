namespace NServiceBus.Features
{
    using System;
    using System.Transactions;
    using Azure.Transports.WindowsAzureServiceBus;
    using Config;
    using Microsoft.ServiceBus;
    using Transports;

    public class AzureServiceBusTransport : ConfigureTransport<AzureServiceBus>
    {
        protected override void InternalConfigure(Configure config)
        {
            Categories.Serializers.SetDefault<JsonSerialization>();

            if (SafeRoleEnvironment.IsAvailable)
            {
                EnableByDefault<QueueAutoCreation>();
            }

            var queuename = AzureServiceBusQueueNamingConvention.Apply(NServiceBus.Configure.EndpointName);

            Address.InitializeLocalAddress(queuename);

            var serverWaitTime = AzureServicebusDefaults.DefaultServerWaitTime;

            var configSection = NServiceBus.Configure.GetConfigSection<AzureServiceBusQueueConfig>();
            if (configSection != null)
                serverWaitTime = configSection.ServerWaitTime;

            // make sure the transaction stays open a little longer than the long poll.
            NServiceBus.Configure.Transactions.Advanced(settings => settings.DefaultTimeout(TimeSpan.FromSeconds(serverWaitTime * 1.1)).IsolationLevel(IsolationLevel.Serializable));


            Enable<AzureServiceBusTransport>();
            EnableByDefault<TimeoutManager>();
            
        }

        public override void Initialize()
        {
            var configSection = NServiceBus.Configure.GetConfigSection<AzureServiceBusQueueConfig>();
            if (configSection == null)
            {
                //hack: just to get the defaults, we should refactor this to support specifying the values on the NServiceBus/Transport connection string as well
                configSection = new AzureServiceBusQueueConfig();
            }

            ServiceBusEnvironment.SystemConnectivity.Mode = (ConnectivityMode)Enum.Parse(typeof(ConnectivityMode), configSection.ConnectivityMode);

            var connectionString = new DeterminesBestConnectionStringForAzureServiceBus().Determine();
            Address.OverrideDefaultMachine(connectionString);

            new ContainerConfiguration().Configure(configSection);
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