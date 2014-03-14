namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus.Config
{
    using System;
    using Features;

    internal class AzureServiceBusGatewayChannel : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (!Feature.IsEnabled<V2.Features.Gateway>()) return;

            var configSection = Configure.GetConfigSection<AzureServiceBusGatewayQueueConfig>() ?? new AzureServiceBusGatewayQueueConfig();

            RegisterDefault<ICreateGatewayQueueClients, AzureServicebusGatewayGatewayQueueClientCreator>(DependencyLifecycle.InstancePerCall, () =>
            {
                Configure.Instance.Configurer.ConfigureProperty<AzureServicebusGatewayGatewayQueueClientCreator>(t => t.PrefetchCount, configSection.PrefetchCount);
            });

            RegisterDefault<ICreateGatewayQueues, AzureServiceBusGatewayQueueCreator>(DependencyLifecycle.InstancePerCall, () =>
            {
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes);
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection);
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.RequiresSession, configSection.RequiresSession);
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueCreator>(t => t.EnablePartitioning, configSection.EnablePartitioning);
            });
            RegisterDefault<ICreateMessagingFactories, CreatesMessagingFactories>(DependencyLifecycle.InstancePerCall, () => { });
            RegisterDefault<ICreateNamespaceManagers, CreatesNamespaceManagers>(DependencyLifecycle.InstancePerCall, () => { });
            RegisterDefault<INotifyReceivedGatewayMessages, AzureServiceBusGatewayQueueNotifier>(DependencyLifecycle.InstancePerCall, () =>
            {
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueNotifier>(t => t.BatchSize, configSection.BatchSize);
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);
            });
            RegisterDefault<ISendGatewayMessages, AzureServiceBusGatewayQueueSender>(DependencyLifecycle.InstancePerCall, () =>
            {
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueSender>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusGatewayQueueSender>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);
            });
        }

        void RegisterDefault<I, T>(DependencyLifecycle lifecycle, Action configure)
        {
            if (!Configure.Instance.Configurer.HasComponent<I>())
            {
                Configure.Instance.Configurer.ConfigureComponent<T>(lifecycle);
                configure();
            }
        }
    }
}
