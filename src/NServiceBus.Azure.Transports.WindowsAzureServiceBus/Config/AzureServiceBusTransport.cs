namespace NServiceBus.Features
{
    using System;
    using Azure.Transports.WindowsAzureServiceBus;
    using Azure.Transports.WindowsAzureServiceBus.QueueAndTopicByEndpoint;
    using Config;
    using Microsoft.ServiceBus;
    using Transports;

    internal class AzureServiceBusTransport : ConfigureTransport
    {
        //internal AzureServiceBusTransport()
        //{
        //    Defaults(a =>
        //    {
        //        var section = a.GetConfigSection<AzureServiceBusQueueConfig>();
        //        a.SetDefault("AzureServiceBus.DefaultConnectionString", defaultconnectionString);
        //    });
        //}

        protected override void Configure(FeatureConfigurationContext context, string defaultconnectionString)
        {
            var configSection = context.Settings.GetConfigSection<AzureServiceBusQueueConfig>();
            if (configSection == null)
            {
                //hack: just to get the defaults, we should refactor this to support specifying the values on the NServiceBus/Transport connection string as well
                configSection = new AzureServiceBusQueueConfig();
            }

            ServiceBusEnvironment.SystemConnectivity.Mode = (ConnectivityMode)Enum.Parse(typeof(ConnectivityMode), configSection.ConnectivityMode);


            var bestConnectionString = new DeterminesBestConnectionStringForAzureServiceBus(defaultconnectionString).Determine(context.Settings);

            // this is  a bug in the core, statics reused across tests
            try // would work on IWantToRunBeforeConfiguration, but would be better to move this method to base configuretransport
            {
                Address.OverrideDefaultMachine(bestConnectionString);
            }
            catch (InvalidOperationException)
            {
                // yes, testing warrants it
            }
           

            var queuename = NamingConventions.QueueNamingConvention(context.Settings, null, context.Settings.EndpointName());
            LocalAddress(queuename);
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