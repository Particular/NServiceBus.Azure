namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System.Configuration;
    using Config;
    using Settings;

    public class DeterminesBestConnectionStringForStorageQueues
    {
        public string Determine(ReadOnlySettings settings)
        {
            var configSection = settings.GetConfigSection<AzureQueueConfig>();
            var connectionString = configSection != null ? configSection.ConnectionString : string.Empty;

            if (string.IsNullOrEmpty(connectionString))
                connectionString = settings.Get<string>("NServiceBus.Transport.ConnectionString");

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

        public string Determine(ReadOnlySettings settings, Address replyToAddress)
        {
            var replyQueue = replyToAddress.Queue;
            var connectionString = replyToAddress.Machine;

            if (!IsPotentialStorageQueueConnectionString(connectionString))
            {
                connectionString = Determine(settings); //todo inject config
            }

            return replyQueue + "@" + connectionString;
        }
    }
}