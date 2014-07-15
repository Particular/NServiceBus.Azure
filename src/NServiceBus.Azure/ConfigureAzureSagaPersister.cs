using NServiceBus.Config;

namespace NServiceBus
{
    using Features;
    using Microsoft.WindowsAzure.Storage;
    using Persistence;
    using SagaPersisters.Azure;
    
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

    public class AzureStorageSagaPersistence : Feature
    {
        internal AzureStorageSagaPersistence()
        {
            DependsOn<Features.Sagas>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var connectionstring = string.Empty;
            var updateSchema = false;

            var configSection = context.Settings.GetConfigSection<AzureSagaPersisterConfig>();

            if (configSection != null)
            {
                connectionstring = configSection.ConnectionString;
                updateSchema = configSection.CreateSchema;
            }

            var account = CloudStorageAccount.Parse(connectionstring);

            context.Container.ConfigureComponent(() => new AzureSagaPersister(account, updateSchema), DependencyLifecycle.InstancePerCall);
        }
    }
}
