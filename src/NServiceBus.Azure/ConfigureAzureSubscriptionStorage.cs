namespace NServiceBus
{
    using Config;
    using Features;
    using Microsoft.WindowsAzure.Storage;
    using Persistence;
    using Unicast.Subscriptions;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static class ConfigureAzureSubscriptionStorage
    {
        /// <summary>
        /// Configures NHibernate Azure Subscription Storage , Settings etc are read from custom config section
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UsePersistence<AzureStorage>()")]
        public static Configure AzureSubscriptionStorage(this Configure config)
        {
            return config.UsePersistence<AzureStorage>();
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// Azure tables are created if requested by the user
        /// </summary>
        /// <param name="config"></param>
        /// <param name="connectionString"></param>
        /// <param name="createSchema"></param>
        /// <param name="tableName"> </param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UsePersistence<AzureStorage>()")]
        public static Configure AzureSubscriptionStorage(this Configure config,
            string connectionString,
            bool createSchema, 
            string tableName)
        {
            return config.UsePersistence<AzureStorage>();
        }        
    }

    public class AzureStorageSubscriptionPersistence : Feature
    {
        internal AzureStorageSubscriptionPersistence()
        {
            DependsOn<MessageDrivenSubscriptions>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<AzureSubscriptionStorageConfig>();
            if (configSection == null) { return; }

            //TODO: get rid of these statics
            SubscriptionServiceContext.SubscriptionTableName = configSection.TableName;
            SubscriptionServiceContext.CreateIfNotExist = configSection.CreateSchema;

            var account = CloudStorageAccount.Parse(configSection.ConnectionString);
            SubscriptionServiceContext.Init(account.CreateCloudTableClient());

            context.Container.ConfigureComponent(() => new AzureSubscriptionStorage(account), DependencyLifecycle.InstancePerCall);
        }
    }
}
