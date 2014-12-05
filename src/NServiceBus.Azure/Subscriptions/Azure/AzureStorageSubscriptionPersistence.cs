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
            Defaults(s =>
            {
                var configSection = s.GetConfigSection<AzureSubscriptionStorageConfig>() ?? new AzureSubscriptionStorageConfig();
                s.SetDefault("AzureSubscriptionStorage.ConnectionString", configSection.ConnectionString);
                s.SetDefault("AzureSubscriptionStorage.TableName", configSection.TableName);
                s.SetDefault("AzureSubscriptionStorage.CreateSchema", configSection.CreateSchema);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            //TODO: get rid of these statics
            SubscriptionServiceContext.SubscriptionTableName = context.Settings.Get<string>("AzureSubscriptionStorage.TableName");
            SubscriptionServiceContext.CreateIfNotExist = context.Settings.Get<bool>("AzureSubscriptionStorage.CreateSchema");

            var account = CloudStorageAccount.Parse(context.Settings.Get<string>("AzureSubscriptionStorage.ConnectionString"));
            SubscriptionServiceContext.Init(account.CreateCloudTableClient());

            context.Container.ConfigureComponent(() => new AzureSubscriptionStorage(account), DependencyLifecycle.InstancePerCall);
        }
    }
}