namespace NServiceBus
{
    using Azure;
    using Config;
    using Features;
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

    public class AzureStorageTimeoutPersistence : Feature
    {
        internal AzureStorageTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<AzureTimeoutPersisterConfig>() ?? new AzureTimeoutPersisterConfig();

            //TODO: get rid of these statics
            ServiceContext.TimeoutDataTableName = configSection.TimeoutDataTableName;
            ServiceContext.TimeoutManagerDataTableName = configSection.TimeoutManagerDataTableName;

            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(tp => tp.ConnectionString, configSection.ConnectionString)
                .ConfigureProperty(tp => tp.CatchUpInterval, configSection.CatchUpInterval)
                .ConfigureProperty(tp => tp.PartitionKeyScope, configSection.PartitionKeyScope);
        }
    }
}