namespace NServiceBus
{
    using System;

    /// <summary>
	/// Contains extension methods to NServiceBus.Configure for the azure blob storage data bus
	/// </summary>
	public static class ConfigureAzureBlobStorageDataBus
	{

        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.EnableFeature<AzureDataBusPersistence>()")]
		public static Configure AzureDataBus(this Configure config)
        {
            throw new InvalidOperationException();
        }
	}
}