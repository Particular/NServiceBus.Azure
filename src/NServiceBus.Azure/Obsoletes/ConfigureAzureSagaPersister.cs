namespace NServiceBus
{
    using Persistence;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the saga persister on top of Azure table storage.
    /// </summary>
    public static class ConfigureAzureSagaPersister
    {
        /// <summary>
        /// Use the table storage backed saga persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UsePersistence<AzureStorage>()")]
        public static Configure AzureSagaPersister(this Configure config)
        {
            return config.UsePersistence<AzureStorage>();
        }

        /// <summary>
        /// Use the table storage backed saga persister implementation on top of Azure table storage.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="connectionString"></param>
        /// <param name="autoUpdateSchema"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UsePersistence<AzureStorage>()")]
        public static Configure AzureSagaPersister(this Configure config,
            string connectionString,
            bool autoUpdateSchema)
        {
            return config.UsePersistence<AzureStorage>();
        }

    }
}
