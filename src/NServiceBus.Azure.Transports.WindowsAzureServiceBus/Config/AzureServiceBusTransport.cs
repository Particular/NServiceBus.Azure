namespace NServiceBus.Features
{
    using System;
    using System.Transactions;
    using Azure.Transports.WindowsAzureServiceBus;
    using Config;
    using Microsoft.ServiceBus;
    using Transports;

    public class AzureServiceBusTransport : ConfigureTransport<AzureServiceBus>
    {
        protected override void InternalConfigure(Configure config)
        {
            Categories.Serializers.SetDefault<JsonSerialization>();

            if (SafeRoleEnvironment.IsAvailable)
            {
                EnableByDefault<QueueAutoCreation>();
            }

            var queuename = AzureServiceBusQueueNamingConvention.Apply(NServiceBus.Configure.EndpointName);

            Address.InitializeLocalAddress(queuename);

            var serverWaitTime = AzureServicebusDefaults.DefaultServerWaitTime;

            var configSection = NServiceBus.Configure.GetConfigSection<AzureServiceBusQueueConfig>();
            if (configSection != null)
                serverWaitTime = configSection.ServerWaitTime;

            // make sure the transaction stays open a little longer than the long poll.
            NServiceBus.Configure.Transactions.Advanced(settings => settings.DefaultTimeout(TimeSpan.FromSeconds(serverWaitTime * 1.1)).IsolationLevel(IsolationLevel.Serializable));


            Enable<AzureServiceBusTransport>();
            EnableByDefault<TimeoutManager>();
            
        }

        public override void Initialize()
        {
            var configSection = NServiceBus.Configure.GetConfigSection<AzureServiceBusQueueConfig>();
            if (configSection == null)
            {
                //hack: just to get the defaults, we should refactor this to support specifying the values on the NServiceBus/Transport connection string as well
                configSection = new AzureServiceBusQueueConfig();
            }

            ServiceBusEnvironment.SystemConnectivity.Mode = (ConnectivityMode)Enum.Parse(typeof(ConnectivityMode), configSection.ConnectivityMode);

            var connectionString = new DeterminesBestConnectionStringForAzureServiceBus().Determine();
            Address.OverrideDefaultMachine(connectionString);

            var config = NServiceBus.Configure.Instance;

            ConfigureCreationInfrastructure(config, configSection);

            config.Configurer.ConfigureComponent<AzureServiceBusDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            

            if (!config.Configurer.HasComponent<ISendMessages>())
            {
                ConfigureSendInfrastructure(config, configSection);
            }

            if (!config.Configurer.HasComponent<IPublishMessages>() &&
                !config.Configurer.HasComponent<IManageSubscriptions>())
            {
                ConfigurePublishingInfrastructure(config, configSection);
            }

           
        }

        static void ConfigurePublishingInfrastructure(Configure config, AzureServiceBusQueueConfig configSection)
        {
            config.Configurer.ConfigureComponent<AzureServiceBusTopicPublisher>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServiceBusTopicPublisher>(t => t.MaxDeliveryCount,configSection.MaxDeliveryCount);

            config.Configurer.ConfigureComponent<AzureServicebusSubscriptionCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServicebusSubscriptionClientCreator>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<AzureServicebusTopicCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServicebusTopicClientCreator>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<AzureServiceBusTopicSubscriptionManager>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<AzureServiceBusSubscriptionNotifier>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServiceBusSubscriptionNotifier>(t => t.ServerWaitTime,configSection.ServerWaitTime);
            config.Configurer.ConfigureProperty<AzureServiceBusSubscriptionNotifier>(t => t.BatchSize, configSection.BatchSize);
            config.Configurer.ConfigureProperty<AzureServiceBusSubscriptionNotifier>(t => t.BackoffTimeInSeconds,configSection.BackoffTimeInSeconds);
        }

        static void ConfigureSendInfrastructure(Configure config, AzureServiceBusQueueConfig configSection)
        {
            config.Configurer.ConfigureComponent<AzureServiceBusMessageQueueSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueSender>(t => t.MaxDeliveryCount,configSection.MaxDeliveryCount);

            config.Configurer.ConfigureComponent<AzureServiceBusQueueCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServicebusQueueClientCreator>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<AzureServiceBusQueueNotifier>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.ServerWaitTime,configSection.ServerWaitTime);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BatchSize, configSection.BatchSize);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BackoffTimeInSeconds,configSection.BackoffTimeInSeconds);
        }

        static void ConfigureCreationInfrastructure(Configure config, AzureServiceBusQueueConfig configSection)
        {
            config.Configurer.ConfigureComponent<CreatesMessagingFactories>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<CreatesNamespaceManagers>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<AzureServiceBusQueueCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.MaxSizeInMegabytes,configSection.MaxSizeInMegabytes);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.RequiresDuplicateDetection,configSection.RequiresDuplicateDetection);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.RequiresSession,configSection.RequiresSession);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.DefaultMessageTimeToLive,TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.EnableDeadLetteringOnMessageExpiration,configSection.EnableDeadLetteringOnMessageExpiration);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.DuplicateDetectionHistoryTimeWindow,TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.MaxDeliveryCount,configSection.MaxDeliveryCount);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.EnableBatchedOperations,configSection.EnableBatchedOperations);

            config.Configurer.ConfigureComponent<AzureServicebusTopicCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServicebusSubscriptionCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.LockDuration,TimeSpan.FromMilliseconds(configSection.LockDuration));
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.RequiresSession,configSection.RequiresSession);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.DefaultMessageTimeToLive,TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.EnableDeadLetteringOnFilterEvaluationExceptions,configSection.EnableDeadLetteringOnFilterEvaluationExceptions);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.MaxDeliveryCount,configSection.MaxDeliveryCount);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.EnableBatchedOperations,configSection.EnableBatchedOperations);
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