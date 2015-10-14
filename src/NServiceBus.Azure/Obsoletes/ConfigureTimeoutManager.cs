namespace NServiceBus
{
    using System;

    public static class ConfigureTimeoutManager
    {
        /// <summary>
        /// Use the in azure timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", ReplacementTypeOrMember = "config.UsePersistence<AzureStorage>()")]
// ReSharper disable UnusedParameter.Global
        public static Configure UseAzureTimeoutPersister(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }
    }
}