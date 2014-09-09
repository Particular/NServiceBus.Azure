namespace NServiceBus.Features
{
    using System;
    using Azure.Transports.WindowsAzureServiceBus;
    using Config;
    using Transports;

    internal class ContainerConfiguration
    {
        public void Configure(FeatureConfigurationContext context, AzureServiceBusQueueConfig configSection)
        {
            ConfigureCreationInfrastructure(context, configSection);

            ConfigureReceiveInfrastructure(context, configSection);

            if (!context.Container.HasComponent<ISendMessages>())
            {
                ConfigureSendInfrastructure(context, configSection);
            }

            if (!context.Container.HasComponent<IPublishMessages>() &&
                !context.Container.HasComponent<IManageSubscriptions>())
            {
                ConfigurePublishingInfrastructure(context, configSection);
            }
        }

        private void ConfigureReceiveInfrastructure(FeatureConfigurationContext context, AzureServiceBusQueueConfig configSection)
        {
            context.Container.ConfigureComponent<AzureServiceBusDequeueStrategy>(DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<AzureServiceBusQueueNotifier>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
            context.Container.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BatchSize, configSection.BatchSize);
            context.Container.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);

            context.Container.ConfigureComponent<AzureServiceBusSubscriptionNotifier>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureProperty<AzureServiceBusSubscriptionNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
            context.Container.ConfigureProperty<AzureServiceBusSubscriptionNotifier>(t => t.BatchSize, configSection.BatchSize);
            context.Container.ConfigureProperty<AzureServiceBusSubscriptionNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);
        }

        private void ConfigurePublishingInfrastructure(FeatureConfigurationContext context, AzureServiceBusQueueConfig configSection)
        {
            context.Container.ConfigureComponent<AzureServiceBusPublisher>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<AzureServiceBusTopicPublisher>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureProperty<AzureServiceBusTopicPublisher>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);

            context.Container.ConfigureComponent<AzureServiceBusTopicSubscriptionManager>(DependencyLifecycle.InstancePerCall);
        }

        private void ConfigureSendInfrastructure(FeatureConfigurationContext context, AzureServiceBusQueueConfig configSection)
        {
            context.Container.ConfigureComponent<AzureServiceBusSender>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<AzureServiceBusQueueSender>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureProperty<AzureServiceBusQueueSender>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
        }

        private void ConfigureCreationInfrastructure(FeatureConfigurationContext context, AzureServiceBusQueueConfig configSection)
        {
            context.Container.ConfigureComponent<CreatesMessagingFactories>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<CreatesNamespaceManagers>(DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent<AzureServicebusQueueClientCreator>(DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<AzureServiceBusTopologyCreator>(DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<AzureServiceBusQueueCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes);
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection);
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.RequiresSession, configSection.RequiresSession);
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
            context.Container.ConfigureProperty<AzureServiceBusQueueCreator>(t => t.EnablePartitioning, configSection.EnablePartitioning);

            context.Container.ConfigureComponent<AzureServicebusTopicClientCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<AzureServicebusTopicCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureProperty<AzureServicebusTopicCreator>(t => t.EnablePartitioning, configSection.EnablePartitioning);

            context.Container.ConfigureComponent<AzureServicebusSubscriptionClientCreator>(DependencyLifecycle.InstancePerCall);
            
            context.Container.ConfigureComponent<AzureServicebusSubscriptionCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
            context.Container.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.RequiresSession, configSection.RequiresSession);
            context.Container.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            context.Container.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
            context.Container.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.EnableDeadLetteringOnFilterEvaluationExceptions, configSection.EnableDeadLetteringOnFilterEvaluationExceptions);
            context.Container.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            context.Container.ConfigureProperty<AzureServicebusSubscriptionCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
        }

    }
}