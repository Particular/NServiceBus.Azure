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
            Defaults(s =>
            {
                var configSection = s.GetConfigSection<AzureDataBusConfig>() ?? new AzureDataBusConfig();
                s.SetDefault("AzureDataBus.Container", configSection.Container);
                s.SetDefault("AzureDataBus.BasePath", configSection.BasePath);
                s.SetDefault("AzureDataBus.ConnectionString", configSection.ConnectionString);
                s.SetDefault("AzureDataBus.MaxRetries", configSection.MaxRetries);
                s.SetDefault("AzureDataBus.BackOffInterval", configSection.BackOffInterval);
                s.SetDefault("AzureDataBus.NumberOfIOThreads", configSection.NumberOfIOThreads);
                s.SetDefault("AzureDataBus.BlockSize", configSection.BlockSize);
                s.SetDefault("AzureDataBus.DefaultTTL", configSection.DefaultTTL);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var cloudBlobClient = CloudStorageAccount.Parse(context.Settings.Get<string>("AzureDataBus.ConnectionString")).CreateCloudBlobClient();

            var dataBus = new BlobStorageDataBus(cloudBlobClient.GetContainerReference(context.Settings.Get<string>("AzureDataBus.Container")))
            {
                BasePath = context.Settings.Get<string>("AzureDataBus.BasePath"),
                MaxRetries = context.Settings.Get<int>("AzureDataBus.MaxRetries"),
                BackOffInterval = context.Settings.Get<int>("AzureDataBus.BackOffInterval"),
                NumberOfIOThreads = context.Settings.Get<int>("AzureDataBus.NumberOfIOThreads"),
                BlockSize = context.Settings.Get<int>("AzureDataBus.BlockSize"),
                DefaultTTL = context.Settings.Get<long>("AzureDataBus.DefaultTTL")
            };

            context.Container.RegisterSingleton<IDataBus>(dataBus);
        }
    }
}