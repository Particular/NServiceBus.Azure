namespace NServiceBus
{
    using Config;
    using Features;
    using Microsoft.WindowsAzure.Storage;
    using Unicast.Subscriptions;

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