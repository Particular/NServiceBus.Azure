namespace NServiceBus
{
    using System;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static partial class ConfigureAzureSubscriptionStorage
    {
        // ReSharper disable UnusedParameter.Global

        /// <summary>
        /// Configures NHibernate Azure Subscription Storage , Settings etc are read from custom config section
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", ReplacementTypeOrMember = "config.UsePersistence<AzureStoragePersistence>()")]
        public static Configure AzureSubscriptionStorage(this Configure config)
        {
            throw new InvalidOperationException();
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
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", ReplacementTypeOrMember = "config.UsePersistence<AzureStoragePersistence>()")]
        public static Configure AzureSubscriptionStorage(this Configure config,

            string connectionString,
            bool createSchema, 
            string tableName)
        {
            throw new InvalidOperationException();
        } 
        
        // ReSharper restore UnusedParameter.Global      
    }
}
