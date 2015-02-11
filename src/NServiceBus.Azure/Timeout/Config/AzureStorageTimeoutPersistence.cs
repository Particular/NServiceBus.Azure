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
            Defaults(s =>
            {
                var config = s.GetConfigSection<AzureTimeoutPersisterConfig>() ?? new AzureTimeoutPersisterConfig();
                s.SetDefault("AzureTimeoutStorage.ConnectionString", config.ConnectionString);
                s.SetDefault("AzureTimeoutStorage.CreateSchema", config.CreateSchema);
                s.SetDefault("AzureTimeoutStorage.TimeoutManagerDataTableName", config.TimeoutManagerDataTableName);
                s.SetDefault("AzureTimeoutStorage.TimeoutDataTableName", config.TimeoutDataTableName);
                s.SetDefault("AzureTimeoutStorage.CatchUpInterval", config.CatchUpInterval);
                s.SetDefault("AzureTimeoutStorage.PartitionKeyScope", config.PartitionKeyScope);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            //TODO: get rid of these statics
            ServiceContext.CreateSchema = context.Settings.Get<bool>("AzureTimeoutStorage.CreateSchema");
            ServiceContext.TimeoutDataTableName = context.Settings.Get<string>("AzureTimeoutStorage.TimeoutDataTableName");
            ServiceContext.TimeoutManagerDataTableName = context.Settings.Get<string>("AzureTimeoutStorage.TimeoutManagerDataTableName");

            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(tp => tp.ConnectionString, context.Settings.Get<string>("AzureTimeoutStorage.ConnectionString"))
                .ConfigureProperty(tp => tp.CatchUpInterval, context.Settings.Get<int>("AzureTimeoutStorage.CatchUpInterval"))
                .ConfigureProperty(tp => tp.PartitionKeyScope, context.Settings.Get<string>("AzureTimeoutStorage.PartitionKeyScope"));
        }
    }
}