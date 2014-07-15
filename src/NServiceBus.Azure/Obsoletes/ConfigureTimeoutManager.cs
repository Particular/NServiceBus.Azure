namespace NServiceBus
{
    using Persistence;

    public static class ConfigureTimeoutManager
    {
        /// <summary>
        /// Use the in azure timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UsePersistence<AzureStorage>()")]
        public static Configure UseAzureTimeoutPersister(this Configure config)
        {
            return config.UsePersistence<AzureStorage>();
        }
    }
}