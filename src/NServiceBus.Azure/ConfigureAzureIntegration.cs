namespace NServiceBus
{
    using NServiceBus.Integration.Azure;

    public static class ConfigureAzureIntegration
    {
        public static void AzureConfigurationSource(this BusConfiguration config, string configurationPrefix = null)
        {
            var azureConfigSource = new AzureConfigurationSource(new AzureConfigurationSettings())
            {
                ConfigurationPrefix = configurationPrefix
            };
            
            config.CustomConfigurationSource(azureConfigSource);
        }
    }
}