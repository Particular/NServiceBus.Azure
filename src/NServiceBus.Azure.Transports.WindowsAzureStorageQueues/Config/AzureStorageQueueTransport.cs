namespace NServiceBus.Features
{
    using Azure.Transports.WindowsAzureStorageQueues;
    using Config;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using ObjectBuilder;
    using Settings;
    using Transports;

    internal class AzureStorageQueueTransport : ConfigureTransport
    {

        protected override void Configure(FeatureConfigurationContext context, string connectionString)
        {
            
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
            context.Container.ConfigureComponent<CreateQueueClients>(DependencyLifecycle.SingleInstance);
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