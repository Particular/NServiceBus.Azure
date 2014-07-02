namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Config;
    using Features;
    using QueueAndTopicByEndpoint;

    public class DefaultTopology : Feature
    {
        public DefaultTopology()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<AzureServiceBusQueueConfig>() ?? new AzureServiceBusQueueConfig();
            var transportConfig = context.Settings.GetConfigSection<TransportConfig>() ?? new TransportConfig();

            new ContainerConfiguration().Configure(context, configSection, transportConfig);

            context.Container.ConfigureComponent(b =>
            {
                var config = b.Build<Configure>();
                var subscriptionCreator = b.Build<AzureServicebusSubscriptionCreator>();
                var topology = new QueueAndTopicByEndpointTopology(config, subscriptionCreator);
                topology.Initialize(context.Settings);
                return topology;

            }, DependencyLifecycle.SingleInstance);

        }
    }
}