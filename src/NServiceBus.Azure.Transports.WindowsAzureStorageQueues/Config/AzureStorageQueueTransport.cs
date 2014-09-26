namespace NServiceBus.Features
{
    using System;
    using Azure.Transports.WindowsAzureStorageQueues;
    using Config;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using ObjectBuilder;
    using Settings;
    using Transports;

    class AzureStorageQueueTransport : ConfigureTransport
    {

        protected override void Configure(FeatureConfigurationContext context, string con)
        {
            context.Settings.Get<Conventions>().AddSystemMessagesConventions(t => typeof(MessageWrapper).IsAssignableFrom(t));

            CloudQueueClient queueClient;

            var configSection = context.Settings.GetConfigSection<AzureQueueConfig>();

            var connectionString = TryGetConnectionString(configSection, con);

            if (string.IsNullOrEmpty(connectionString))
            {
                queueClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();     
            }
            else
            {
                queueClient = CloudStorageAccount.Parse(connectionString).CreateCloudQueueClient();

                try
                {
                    Address.OverrideDefaultMachine(connectionString);  
                }
                catch (InvalidOperationException)
                {
                    //swallow till refactored
                }
                              
            }

            context.Container.RegisterSingleton(queueClient);

            var receiverConfig = context.Container.ConfigureComponent<AzureMessageQueueReceiver>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<CreateQueueClients>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<AzureMessageQueueSender>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<PollingDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<AzureMessageQueueCreator>(DependencyLifecycle.InstancePerCall);

            if (configSection != null)
            {
                context.Container.ConfigureProperty<AzureMessageQueueReceiver>(t => t.PurgeOnStartup, configSection.PurgeOnStartup);
                context.Container.ConfigureProperty<AzureMessageQueueReceiver>(t => t.PurgeOnStartup, configSection.PurgeOnStartup);
                context.Container.ConfigureProperty<AzureMessageQueueReceiver>(t => t.MaximumWaitTimeWhenIdle, configSection.MaximumWaitTimeWhenIdle);
                context.Container.ConfigureProperty<AzureMessageQueueReceiver>(t => t.MessageInvisibleTime, configSection.MessageInvisibleTime);
                context.Container.ConfigureProperty<AzureMessageQueueReceiver>(t => t.PeekInterval, configSection.PeekInterval);
                context.Container.ConfigureProperty<AzureMessageQueueReceiver>(t => t.BatchSize, configSection.BatchSize);
            }

            
            context.Settings.ApplyTo<AzureMessageQueueReceiver>((IComponentConfig)receiverConfig);
        }

        protected override string GetLocalAddress(ReadOnlySettings settings)
        {
            return AzureQueueNamingConvention.Apply(settings);
        }

        static string TryGetConnectionString(AzureQueueConfig configSection, string defaultConnectionString)
        {
            var connectionString = defaultConnectionString;

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