namespace NServiceBus.Azure.Transports.ServiceBus
{
    using System.Configuration;
    using Config;
    using Settings;

    public class DeterminesBestConnectionStringForAzureServiceBus
    {
        public string Determine()
        {
            var configSection = Configure.GetConfigSection<AzureServiceBusQueueConfig>();
            var connectionString = configSection != null ? configSection.ConnectionString : string.Empty;

            if (string.IsNullOrEmpty(connectionString))
                connectionString = SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");

            if (configSection != null && !string.IsNullOrEmpty(configSection.IssuerKey) && !string.IsNullOrEmpty(configSection.ServiceNamespace))
                connectionString = string.Format("Endpoint=sb://{0}.servicebus.windows.net/;SharedSecretIssuer=owner;SharedSecretValue={1}", configSection.ServiceNamespace, configSection.IssuerKey);

            if (string.IsNullOrEmpty(connectionString) && (configSection == null || string.IsNullOrEmpty(configSection.IssuerKey) || string.IsNullOrEmpty(configSection.ServiceNamespace)))
            {
                throw new ConfigurationErrorsException("No Servicebus Connection information specified, either set the ConnectionString or set the IssuerKey and ServiceNamespace properties");
            }

            return connectionString;
        }

        public bool IsPotentialServiceBusConnectionString(string potentialConnectionString)
        {
            return potentialConnectionString.StartsWith("Endpoint=sb://");
        }

        public string Determine(Address replyToAddress)
        {
            if (IsPotentialServiceBusConnectionString(replyToAddress.Machine))
            {
                return replyToAddress.ToString();
            }
            else
            {
                var replyQueue = replyToAddress.Queue;
                var @namespace = Determine();
                return replyQueue + "@" + @namespace;
            }
        }
    }
}