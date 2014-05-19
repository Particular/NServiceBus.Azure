namespace NServiceBus
{
    using Config;
    using DataBus;
    using DataBus.Azure.BlobStorage;
    using CloudStorageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount;

    /// <summary>
	/// Contains extension methods to NServiceBus.Configure for the azure blob storage data bus
	/// </summary>
	public static class ConfigureAzureBlobStorageDataBus
	{
	    public const string Defaultcontainer = "databus";
        public const string DefaultBasePath = "";
        public const int DefaultMaxRetries = 5;
        public const int DefaultNumberOfIOThreads = 5;
	    public const string DefaultConnectionString = "UseDevelopmentStorage=true";
	    public const int DefaultBlockSize = 4*1024*1024; // 4MB
		
		public static Configure AzureDataBus(this Configure config)
		{
            var configSection = config.GetConfigSection<AzureDataBusConfig>() ?? new AzureDataBusConfig();

            var cloudBlobClient = CloudStorageAccount.Parse(configSection.ConnectionString).CreateCloudBlobClient();

            var dataBus = new BlobStorageDataBus(cloudBlobClient.GetContainerReference(configSection.Container))
            {
                BasePath = configSection.BasePath,
                MaxRetries = configSection.MaxRetries,
                NumberOfIOThreads = configSection.NumberOfIOThreads,
                BlockSize = configSection.BlockSize
            };

		    config.Configurer.RegisterSingleton<IDataBus>(dataBus);

			return config;
		}
	}
}