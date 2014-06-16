using NServiceBus.Integration.Azure;

namespace NServiceBus
{
    public static class ConfigureAzureIntegration
    {
        public static void AzureConfigurationSource(this Configure.ConfigurationBuilder config, string configurationPrefix = null)
        {
            var azureConfigSource = new AzureConfigurationSource(new AzureConfigurationSettings())
            {
                ConfigurationPrefix = configurationPrefix
            };
            
            config.CustomConfigurationSource(azureConfigSource);
        }
    }
}