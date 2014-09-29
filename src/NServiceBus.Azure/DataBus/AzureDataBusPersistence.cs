namespace NServiceBus
{
    using Config;
    using DataBus;
    using DataBus.Azure.BlobStorage;
    using Features;
    using Microsoft.WindowsAzure.Storage;

    public class AzureDataBusPersistence : Feature
    {
        internal AzureDataBusPersistence()
        {
            DependsOn<Features.DataBus>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<AzureDataBusConfig>() ?? new AzureDataBusConfig();

            var cloudBlobClient = CloudStorageAccount.Parse(configSection.ConnectionString).CreateCloudBlobClient();

            var dataBus = new BlobStorageDataBus(cloudBlobClient.GetContainerReference(configSection.Container))
            {
                BasePath = configSection.BasePath,
                MaxRetries = configSection.MaxRetries,
                NumberOfIOThreads = configSection.NumberOfIOThreads,
                BlockSize = configSection.BlockSize
            };

            context.Container.RegisterSingleton<IDataBus>(dataBus);
        }
    }
}