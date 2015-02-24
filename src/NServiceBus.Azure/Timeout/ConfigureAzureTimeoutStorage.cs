namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;
    using Persistence;
    using Timeout;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static class ConfigureAzureTimeoutStorage
    {
        /// <summary>
        /// Connection string to use for timeouts storage.
        /// </summary>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> ConnectionString(this PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> config, string connectionString)
        {
            AzureTimeoutStorageGuard.CheckConnectionString(connectionString);

            config.GetSettings().Set("AzureTimeoutStorage.ConnectionString", connectionString);
            return config;
        }

        /// <summary>
        /// Should an attempt at startup be made to verify if storage tables for timeouts exist or not and if not create those.
        /// <remarks>Operation will fail if connection string does not allow access to create storage tables</remarks>
        /// </summary>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> CreateSchema(this PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> config, bool createSchema)
        {
            config.GetSettings().Set("AzureTimeoutStorage.CreateSchema", createSchema);
            return config;
        }

        /// <summary>
        /// Set the name of the table where the timeout manager stores it's internal state.
        /// </summary>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> TimeoutManagerDataTableName(this PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            AzureTimeoutStorageGuard.CheckTableName(tableName);

            config.GetSettings().Set("AzureTimeoutStorage.TimeoutManagerDataTableName", tableName);
            return config;
        }

        /// <summary>
        ///  Set the name of the table where the timeouts themselves are stored.
        /// </summary>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> TimeoutDataTableName(this PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> config, string tableName)
        {
            AzureTimeoutStorageGuard.CheckTableName(tableName);

            config.GetSettings().Set("AzureTimeoutStorage.TimeoutDataTableName", tableName);
            return config;
        }

        /// <summary>
        ///  Set the catchup interval in seconds for missed timeouts.
        /// </summary>
        /// <param name="catchUpInterval">Catch up interval in seconds</param>
        /// <param name="config"></param>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> CatchUpInterval(this PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> config, int catchUpInterval)
        {
            AzureTimeoutStorageGuard.CheckCatchUpInterval(catchUpInterval);

            config.GetSettings().Set("AzureTimeoutStorage.CatchUpInterval", catchUpInterval);
            return config;
        }

        /// <summary>
        ///  Time range used as partition key value for all timeouts.
        /// </summary>
        /// <param name="partitionKeyScope">Partition key DateTime format string.</param>
        /// <param name="config"></param>
        /// <remarks>For optimal performance, this should be in line with the CatchUpInterval.</remarks>
        public static PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> PartitionKeyScope(this PersistenceExtentions<AzureStoragePersistence, StorageType.Timeouts> config, string partitionKeyScope)
        {
            AzureTimeoutStorageGuard.CheckPartitionKeyScope(partitionKeyScope);

            config.GetSettings().Set("AzureTimeoutStorage.PartitionKeyScope", partitionKeyScope);
            return config;
        }
    }
}
