namespace NServiceBus.Features
{
    using System;
    using System.Transactions;
    using Azure.Transports.WindowsAzureServiceBus;
    using Config;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
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

            ServiceBusEnvironment.SystemConnectivity.Mode = configSection == null ? ConnectivityMode.Tcp : (ConnectivityMode)Enum.Parse(typeof(ConnectivityMode), configSection.ConnectivityMode);

            var connectionString = new DeterminesBestConnectionStringForAzureServiceBus().Determine();

            var namespaceClient = NamespaceManager.CreateFromConnectionString(connectionString);
            var factory = MessagingFactory.CreateFromConnectionString(connectionString);
            
            Address.OverrideDefaultMachine(connectionString);

            NServiceBus.Configure.Instance.Configurer.RegisterSingleton<NamespaceManager>(namespaceClient);
            NServiceBus.Configure.Instance.Configurer.RegisterSingleton<MessagingFactory>(factory);
            NServiceBus.Configure.Component<AzureServiceBusQueueCreator>(DependencyLifecycle.InstancePerCall);

            var config = NServiceBus.Configure.Instance;

            config.Configurer.ConfigureComponent<AzureServiceBusDequeueStrategy>(DependencyLifecycle.InstancePerCall);

            if (configSection == null)
            {
                //hack: just to get the defaults, we should refactor this to support specifying the values on the NServiceBus/Transport connection string as well
                configSection = new AzureServiceBusQueueConfig();
            }

            if (!config.Configurer.HasComponent<ISendMessages>())
            {
                config.Configurer.ConfigureComponent<AzureServiceBusMessageQueueSender>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueSender>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);

                config.Configurer.ConfigureComponent<AzureServiceBusQueueNotifier>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServicebusQueueClientCreator>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
                config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes);
                config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection);
                config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.RequiresSession, configSection.RequiresSession);
                config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
                config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
                config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
                config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
                config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
                config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BatchSize, configSection.BatchSize);
                config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);

            }

            if (!config.Configurer.HasComponent<IPublishMessages>() &&
                !config.Configurer.HasComponent<IManageSubscriptions>())
            {
                config.Configurer.ConfigureComponent<AzureServicebusSubscriptionClientCreator>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServiceBusTopicSubscriptionManager>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServiceBusTopicPublisher>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServiceBusTopicNotifier>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServicebusTopicClientCreator>(DependencyLifecycle.InstancePerCall);
                
                config.Configurer.ConfigureProperty<AzureServiceBusTopicPublisher>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.RequiresSession, configSection.RequiresSession);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.EnableDeadLetteringOnFilterEvaluationExceptions, configSection.EnableDeadLetteringOnFilterEvaluationExceptions);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
                config.Configurer.ConfigureProperty<AzureServiceBusTopicNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
                config.Configurer.ConfigureProperty<AzureServiceBusTopicNotifier>(t => t.BatchSize, configSection.BatchSize);
                config.Configurer.ConfigureProperty<AzureServiceBusTopicNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);
            }

           
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