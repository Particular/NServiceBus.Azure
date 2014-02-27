namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System.Configuration;
    using Config;
    using Settings;

    public class DeterminesBestConnectionStringForStorageQueues
    {
        public string Determine()
        {
            var configSection = Configure.GetConfigSection<AzureQueueConfig>();
            var connectionString = configSection != null ? configSection.ConnectionString : string.Empty;

            if (string.IsNullOrEmpty(connectionString))
                connectionString = SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ConfigurationErrorsException("No Azure Storage Connection information specified, please set the ConnectionString");
            }

            return connectionString;
        }

        public bool IsPotentialStorageQueueConnectionString(string potentialConnectionString)
        {
            return potentialConnectionString.StartsWith("UseDevelopmentStorage=true") ||
                potentialConnectionString.StartsWith("DefaultEndpointsProtocol=https");
        }

        public string Determine(Address replyToAddress)
        {
            var replyQueue = replyToAddress.Queue;
            var connectionString = replyToAddress.Machine;

            if (!IsPotentialStorageQueueConnectionString(connectionString))
            {
                connectionString = Determine();
            }

            return replyQueue + "@" + connectionString;
        }
    }
}