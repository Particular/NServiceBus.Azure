namespace NServiceBus.DataBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;

    public static class ConfigureAzureDataBus
    {
        public static DataBusExtentions<AzureDataBus> MaxRetries(this DataBusExtentions<AzureDataBus> config, int maxRetries)
        {
            AzureDataBusGuard.CheckMaxRetries(maxRetries);

            config.GetSettings().Set("AzureDataBus.MaxRetries", maxRetries);
            return config;
        }

        public static DataBusExtentions<AzureDataBus> BackOffInterval(this DataBusExtentions<AzureDataBus> config, int backOffInterval)
        {
            AzureDataBusGuard.CheckBackOffInterval(backOffInterval);

            config.GetSettings().Set("AzureDataBus.BackOffInterval", backOffInterval);
            return config;
        }

        public static DataBusExtentions<AzureDataBus> BlockSize(this DataBusExtentions<AzureDataBus> config, int blockSize)
        {
            AzureDataBusGuard.CheckBlockSize(blockSize);

            config.GetSettings().Set("AzureDataBus.BlockSize", blockSize);
            return config;
        }

        public static DataBusExtentions<AzureDataBus> NumberOfIOThreads(this DataBusExtentions<AzureDataBus> config, int numberOfIOThreads)
        {
            AzureDataBusGuard.CheckNumberOfIOThreads(numberOfIOThreads);

            config.GetSettings().Set("AzureDataBus.NumberOfIOThreads", numberOfIOThreads);
            return config;
        }

        public static DataBusExtentions<AzureDataBus> ConnectionString(this DataBusExtentions<AzureDataBus> config, string connectionString)
        {
            AzureDataBusGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set("AzureDataBus.ConnectionString", connectionString);
            return config;
        }

        public static DataBusExtentions<AzureDataBus> Container(this DataBusExtentions<AzureDataBus> config, string containerName)
        {
            AzureDataBusGuard.CheckContainerName(containerName);

            config.GetSettings().Set("AzureDataBus.Container", containerName);
            return config;
        }

        public static DataBusExtentions<AzureDataBus> BasePath(this DataBusExtentions<AzureDataBus> config, string basePath)
        {
            AzureDataBusGuard.CheckBasePath(basePath);

            config.GetSettings().Set("AzureDataBus.BasePath", basePath);
            return config;
        }

        public static DataBusExtentions<AzureDataBus> DefaultTTL(this DataBusExtentions<AzureDataBus> config, string defaultTTL)
        {
            AzureDataBusGuard.CheckDefaultTTL(defaultTTL);

            config.GetSettings().Set("AzureDataBus.DefaultTTL", defaultTTL);
            return config;
        }
    }
}
