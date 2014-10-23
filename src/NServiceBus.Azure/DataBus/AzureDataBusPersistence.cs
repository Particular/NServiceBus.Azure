namespace NServiceBus
{
    using Config;
    using DataBus;
    using DataBus.Azure.BlobStorage;
    using Features;
    using Microsoft.WindowsAzure.Storage;

    [ObsoleteEx(
        Replacement = "UseDataBusExtensions.UseDataBus<AzureDataBus>(this BusConfiguration config)",
        Message = "Use `configuration.UseDataBus<AzureDataBus>()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.5")]
    // TODO: once obsoleted, internalize class and move into NServiceBus.Features namespace for consistency with Core
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