namespace NServiceBus
{
    using Config;
    using Features;
    using Microsoft.WindowsAzure.Storage;
    using SagaPersisters.Azure;

    public class AzureStorageSagaPersistence : Feature
    {
        internal AzureStorageSagaPersistence()
        {
            DependsOn<Features.Sagas>();
            Defaults(s =>
            {
                var configSection = s.GetConfigSection<AzureSagaPersisterConfig>() ?? new AzureSagaPersisterConfig();
                s.SetDefault("AzureSagaStorage.ConnectionString", configSection.ConnectionString);
                s.SetDefault("AzureSagaStorage.CreateSchema", configSection.CreateSchema);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var connectionstring = context.Settings.Get<string>("AzureSagaStorage.ConnectionString");
            var updateSchema = context.Settings.Get<bool>("AzureSagaStorage.CreateSchema");

            var account = CloudStorageAccount.Parse(connectionstring);

            context.Container.ConfigureComponent(() => new AzureSagaPersister(account, updateSchema), DependencyLifecycle.InstancePerCall);
        }
    }
}