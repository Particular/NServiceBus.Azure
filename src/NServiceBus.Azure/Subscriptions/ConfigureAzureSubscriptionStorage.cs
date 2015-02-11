namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Subscriptions;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static partial class ConfigureAzureSubscriptionStorage
    {
        /// <summary>
        /// Connection string to use for subscriptions storage.
        /// </summary>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Subscriptions> ConnectionString(this PersistenceExtentions<AzureStoragePersistence, StorageType.Subscriptions> config, string connectionString)
        {
            AzureSubscriptionStorageGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set("AzureSubscriptionStorage.ConnectionString", connectionString);
            return config;
        }

        /// <summary>
        /// Table name to create in Azure storage account to persist subscriptions.
        /// </summary>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Subscriptions> TableName(this PersistenceExtentions<AzureStoragePersistence, StorageType.Subscriptions> config, string tableName)
        {
            AzureSubscriptionStorageGuard.CheckTableName(tableName);

            config.GetSettings().Set("AzureSubscriptionStorage.TableName", tableName);
            return config;
        }

        /// <summary>
        /// Should an attempt at startup be made to verify if subscriptions storage table exists or not and if not create it.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Subscriptions> CreateSchema(this PersistenceExtentions<AzureStoragePersistence, StorageType.Subscriptions> config, bool createSchema)
        {
            config.GetSettings().Set("AzureSubscriptionStorage.CreateSchema", createSchema);
            return config;
        }
    }
}
