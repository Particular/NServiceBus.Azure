﻿namespace NServiceBus.Features
{
    using Azure.Transports.WindowsAzureStorageQueues;
    using Config;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Settings;
    using Transports;

    public class AzureStorageQueueTransport : ConfigureTransport<AzureStorageQueue>
    {
        protected override void InternalConfigure(Configure config)
        {
            Enable<AzureStorageQueueTransport>();
            EnableByDefault<MessageDrivenSubscriptions>();
            EnableByDefault<StorageDrivenPublisher>();
            EnableByDefault<TimeoutManager>();
            Categories.Serializers.SetDefault<JsonSerialization>();

            SettingsHolder.SetDefault("ScaleOut.UseSingleBrokerQueue", true); // default to one queue for all instances

            var configSection = NServiceBus.Configure.GetConfigSection<AzureQueueConfig>();

            if(configSection == null)
                return;

            SettingsHolder.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.PurgeOnStartup, configSection.PurgeOnStartup);
            SettingsHolder.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.MaximumWaitTimeWhenIdle, configSection.MaximumWaitTimeWhenIdle);
            SettingsHolder.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.MessageInvisibleTime, configSection.MessageInvisibleTime);
            SettingsHolder.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.PeekInterval, configSection.PeekInterval);
            SettingsHolder.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.BatchSize, configSection.BatchSize);
        }

        public override void Initialize()
        {
            CloudQueueClient queueClient;

            var configSection = NServiceBus.Configure.GetConfigSection<AzureQueueConfig>();

            var connectionString = TryGetConnectionString(configSection);

            if (string.IsNullOrEmpty(connectionString))
            {
                queueClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();     
            }
            else
            {
                queueClient = CloudStorageAccount.Parse(connectionString).CreateCloudQueueClient();

                Address.OverrideDefaultMachine(connectionString);                
            }

            NServiceBus.Configure.Instance.Configurer.RegisterSingleton<CloudQueueClient>(queueClient);

            NServiceBus.Configure.Component<AzureMessageQueueReceiver>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);
            NServiceBus.Configure.Component<AzureMessageQueueSender>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<PollingDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<AzureMessageQueueCreator>(DependencyLifecycle.InstancePerCall);

            var queuename = AzureQueueNamingConvention.Apply(NServiceBus.Configure.EndpointName);
            SettingsHolder.ApplyTo<AzureMessageQueueReceiver>();
            Address.InitializeLocalAddress(queuename);
        }

        static string TryGetConnectionString(AzureQueueConfig configSection)
        {
            var connectionString = SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");

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