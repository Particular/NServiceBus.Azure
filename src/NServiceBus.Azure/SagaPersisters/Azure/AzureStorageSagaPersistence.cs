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