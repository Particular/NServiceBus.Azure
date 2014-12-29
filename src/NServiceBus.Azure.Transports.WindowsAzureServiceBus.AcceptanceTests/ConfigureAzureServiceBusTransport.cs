using NServiceBus;
using NServiceBus.Azure.Transports.WindowsAzureServiceBus;

public class ConfigureAzureServiceBusTransport
{
    public void Configure(BusConfiguration config)
    {
        config.DisableFeature<DefaultTopology>();
        config.EnableFeature<NewTopology>();

        config.UseTransport<AzureServiceBusTransport>()
            //not used for the momemnt, connectionstrings hardcoded in new topology (for now)
            .ConnectionString("Endpoint=sb://topobybundle1.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=F+2isy9V5X2gzwy01NL261ljAIxF7BJ2PSi0518AkI4=");
    }
}