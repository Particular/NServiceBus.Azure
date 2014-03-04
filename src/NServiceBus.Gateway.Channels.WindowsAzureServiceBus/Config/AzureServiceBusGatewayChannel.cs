namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus.Config
{
    using Features;

    internal class AzureServiceBusGatewayChannel : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (!Feature.IsEnabled<Gateway>()) return;

            RegisterDefault<ICreateGatewayQueueClients, AzureServicebusGatewayGatewayQueueClientCreator>(DependencyLifecycle.InstancePerCall);
            RegisterDefault<ICreateGatewayQueues, AzureServiceBusGatewayQueueCreator>(DependencyLifecycle.InstancePerCall);
            RegisterDefault<ICreateMessagingFactories, CreatesMessagingFactories>(DependencyLifecycle.InstancePerCall);
            RegisterDefault<ICreateNamespaceManagers, CreatesNamespaceManagers>(DependencyLifecycle.InstancePerCall);
            RegisterDefault<INotifyReceivedGatewayMessages, AzureServiceBusGatewayQueueNotifier>(DependencyLifecycle.InstancePerCall);
            RegisterDefault<ISendGatewayMessages, AzureServiceBusGatewayQueueSender>(DependencyLifecycle.InstancePerCall);
        }

        void RegisterDefault<I, T>(DependencyLifecycle lifecycle)
        {
            if (!Configure.Instance.Configurer.HasComponent<I>())
            {
                Configure.Instance.Configurer.ConfigureComponent<T>(lifecycle);
            }
        }
    }
}
