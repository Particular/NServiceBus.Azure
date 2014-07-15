namespace NServiceBus
{
    using Persistence;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static class ConfigureAzureSubscriptionStorage
    {
        /// <summary>
        /// Configures NHibernate Azure Subscription Storage , Settings etc are read from custom config section
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UsePersistence<AzureStorage>()")]
        public static Configure AzureSubscriptionStorage(this Configure config)
        {
            return config.UsePersistence<AzureStorage>();
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// Azure tables are created if requested by the user
        /// </summary>
        /// <param name="config"></param>
        /// <param name="connectionString"></param>
        /// <param name="createSchema"></param>
        /// <param name="tableName"> </param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UsePersistence<AzureStorage>()")]
        public static Configure AzureSubscriptionStorage(this Configure config,
            string connectionString,
            bool createSchema, 
            string tableName)
        {
            return config.UsePersistence<AzureStorage>();
        }        
    }
}
