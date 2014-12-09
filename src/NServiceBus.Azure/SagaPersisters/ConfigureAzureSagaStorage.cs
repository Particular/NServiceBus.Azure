namespace NServiceBus.SagaPersisters
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;

    /// <summary>
    /// Configuration extensions for the saga storage
    /// </summary>
    public static class ConfigureAzureSagaStorage
    {
        /// <summary>
        /// Connection string to use for subscriptions storage.
        /// </summary>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Sagas> ConnectionString(this PersistenceExtentions<AzureStoragePersistence, StorageType.Sagas> config, string connectionString)
        {
            AzureStorageSagaGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set("AzureSagaStorage.ConnectionString", connectionString);
            return config;
        }

        /// <summary>
        /// Should an attempt at startup be made to verify if subscriptions storage table exists or not and if not create it.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Sagas> CreateSchema(this PersistenceExtentions<AzureStoragePersistence, StorageType.Sagas> config, bool createSchema)
        {
            config.GetSettings().Set("AzureSagaStorage.CreateSchema", createSchema);
            return config;
        }

    }
}