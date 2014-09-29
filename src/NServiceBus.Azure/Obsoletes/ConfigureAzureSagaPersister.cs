namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the saga persister on top of Azure table storage.
    /// </summary>
    public static class ConfigureAzureSagaPersister
    {
        // ReSharper disable UnusedParameter.Global
        
        /// <summary>
        /// Use the table storage backed saga persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UsePersistence<AzureStorage>()")]
        public static Configure AzureSagaPersister(this Configure config)
        {
            throw new InvalidOperationException();
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
            throw new InvalidOperationException();
        }
        // ReSharper restore UnusedParameter.Global
    }
}
