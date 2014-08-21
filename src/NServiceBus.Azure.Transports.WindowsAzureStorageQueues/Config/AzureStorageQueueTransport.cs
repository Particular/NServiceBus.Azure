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
        internal AzureStorageQueueTransport()
        {
            Defaults(settings =>
            {
                var configSection = settings.GetConfigSection<AzureQueueConfig>();

                if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
                {
                    if (configSection.QueuePerInstance)
                    {
                        settings.SetDefault("ScaleOut.UseSingleBrokerQueue", false);
                    }
                }
            });
        }

        protected override void Configure(FeatureConfigurationContext context, string con)
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

            context.Container.RegisterSingleton(queueClient);

            var receiverConfig = context.Container.ConfigureComponent<AzureMessageQueueReceiver>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<CreateQueueClients>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<AzureMessageQueueSender>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<PollingDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<AzureMessageQueueCreator>(DependencyLifecycle.InstancePerCall);

            var queuename = AzureQueueNamingConvention.Apply(context.Settings);
            context.Settings.ApplyTo<AzureMessageQueueReceiver>((IComponentConfig)receiverConfig);

            LocalAddress(queuename);
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