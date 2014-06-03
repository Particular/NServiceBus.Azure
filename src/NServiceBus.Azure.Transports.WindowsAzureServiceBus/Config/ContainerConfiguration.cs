namespace NServiceBus.Features
{
    using System;
    using Azure.Transports.WindowsAzureServiceBus;
    using Config;
    using Transports;

    internal class ContainerConfiguration
    {
        public void Configure(AzureServiceBusQueueConfig configSection, TransportConfig transportConfig)
        {
            var config = NServiceBus.Configure.Instance;

            ConfigureCreationInfrastructure(config, configSection, transportConfig);

            ConfigureReceiveInfrastructure(config, configSection);

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

        private void ConfigureReceiveInfrastructure(Configure config, AzureServiceBusQueueConfig configSection)
        {
            config.Configurer.ConfigureComponent<AzureServiceBusDequeueStrategy>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<AzureServiceBusQueueNotifier>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BatchSize, configSection.BatchSize);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);

            config.Configurer.ConfigureComponent<AzureServiceBusSubscriptionNotifier>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServiceBusSubscriptionNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
            config.Configurer.ConfigureProperty<AzureServiceBusSubscriptionNotifier>(t => t.BatchSize, configSection.BatchSize);
            config.Configurer.ConfigureProperty<AzureServiceBusSubscriptionNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);
        }

        private void ConfigurePublishingInfrastructure(Configure config, AzureServiceBusQueueConfig configSection)
        {
            config.Configurer.ConfigureComponent<AzureServiceBusTopicPublisher>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServiceBusTopicPublisher>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            
            config.Configurer.ConfigureComponent<AzureServiceBusTopicSubscriptionManager>(DependencyLifecycle.InstancePerCall);
        }

        private void ConfigureSendInfrastructure(Configure config, AzureServiceBusQueueConfig configSection)
        {
            config.Configurer.ConfigureComponent<AzureServiceBusMessageQueueSender>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueSender>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
        }

        private void ConfigureCreationInfrastructure(Configure config, AzureServiceBusQueueConfig configSection, TransportConfig transportConfig)
        {
            config.Configurer.ConfigureComponent<CreatesMessagingFactories>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<CreatesNamespaceManagers>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<AzureServicebusQueueClientCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.MaxRetries, transportConfig.MaxRetries);

            config.Configurer.ConfigureComponent<AzureServiceBusQueueCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.RequiresSession, configSection.RequiresSession);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
            config.Configurer.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.EnablePartitioning, configSection.EnablePartitioning);

            config.Configurer.ConfigureComponent<AzureServicebusTopicClientCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServicebusTopicCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServicebusTopicCreator>(t => t.EnablePartitioning, configSection.EnablePartitioning);

            config.Configurer.ConfigureComponent<AzureServicebusSubscriptionClientCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.MaxRetries, transportConfig.MaxRetries);

            config.Configurer.ConfigureComponent<AzureServicebusSubscriptionCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.RequiresSession, configSection.RequiresSession);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.EnableDeadLetteringOnFilterEvaluationExceptions, configSection.EnableDeadLetteringOnFilterEvaluationExceptions);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            config.Configurer.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
        }

    }
}