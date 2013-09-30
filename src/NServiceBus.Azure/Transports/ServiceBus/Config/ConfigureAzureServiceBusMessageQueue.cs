namespace NServiceBus
{
    public static class ConfigureAzureServiceBusMessageQueue
    {
        public static Configure AzureServiceBusMessageQueue(this Configure config)
        {
            return config.UseTransport<AzureServiceBus>();
        }
    }
}