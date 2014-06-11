namespace NServiceBus.Features
{
    using Azure.Transports.WindowsAzureStorageQueues;
    using Config;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using ObjectBuilder;
    using Settings;
    using Transports;

    internal class AzureStorageQueueTransport : ConfigureTransport<AzureStorageQueue>
    {
        protected override void InternalConfigure(Configure config)
        {
            config.Features(f =>
            {
                f.Enable<AzureStorageQueueTransport>();
                f.Enable<TimeoutManagerBasedDeferral>();
            });
            config.Settings.EnableFeatureByDefault<MessageDrivenSubscriptions>();
            config.Settings.EnableFeatureByDefault<StorageDrivenPublishing>();
            config.Settings.EnableFeatureByDefault<TimeoutManager>();

            config.Settings.SetDefault("SelectedSerializer", typeof(Json));

            config.Settings.SetDefault("ScaleOut.UseSingleBrokerQueue", true); // default to one queue for all instances

            var configSection = config.Settings.GetConfigSection<AzureQueueConfig>();

            if(configSection == null)
                return;

            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.PurgeOnStartup, configSection.PurgeOnStartup);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.MaximumWaitTimeWhenIdle, configSection.MaximumWaitTimeWhenIdle);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.MessageInvisibleTime, configSection.MessageInvisibleTime);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.PeekInterval, configSection.PeekInterval);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.BatchSize, configSection.BatchSize);
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            CloudQueueClient queueClient;

            var configSection = context.Settings.GetConfigSection<AzureQueueConfig>();

            var connectionString = TryGetConnectionString(configSection, context.Settings);

            if (string.IsNullOrEmpty(connectionString))
            {
                queueClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();     
            }
            else
            {
                queueClient = CloudStorageAccount.Parse(connectionString).CreateCloudQueueClient();

                Address.OverrideDefaultMachine(connectionString);                
            }

            context.Container.RegisterSingleton<CloudQueueClient>(queueClient);

            var recieverConfig = context.Container.ConfigureComponent<AzureMessageQueueReceiver>(DependencyLifecycle.InstancePerCall);
            recieverConfig.ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);
            context.Container.ConfigureComponent<AzureMessageQueueSender>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<PollingDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<AzureMessageQueueCreator>(DependencyLifecycle.InstancePerCall);

            var queuename = AzureQueueNamingConvention.Apply(context.Settings.EndpointName());
            context.Settings.ApplyTo<AzureMessageQueueReceiver>((IComponentConfig)recieverConfig);
            Address.InitializeLocalAddress(queuename);
        }

        static string TryGetConnectionString(AzureQueueConfig configSection, ReadOnlySettings config)
        {
            var connectionString = config.Get<string>("NServiceBus.Transport.ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                if (configSection != null)
                {
                    connectionString = configSection.ConnectionString;
                }
            }

            return connectionString;
        }

        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "todo - refactor the transport to use a connection string instead of a custom section"; }
        }


    }
}