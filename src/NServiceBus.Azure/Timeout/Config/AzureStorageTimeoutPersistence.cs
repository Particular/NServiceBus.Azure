namespace NServiceBus
{
    using Azure;
    using Config;
    using Features;

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