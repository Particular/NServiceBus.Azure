namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Config;
    using Features;
    using QueueAndTopicByEndpoint;
    using Transports;

    public class DefaultTopology : Feature
    {
        public DefaultTopology()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<AzureServiceBusQueueConfig>() ?? new AzureServiceBusQueueConfig();

            new ContainerConfiguration().Configure(context, configSection);

            context.Container.ConfigureComponent(b =>
            {
                var config = b.Build<Configure>();
                var messagingFactories = b.Build<ICreateMessagingFactories>();
                var namespaceManagers = b.Build<ICreateNamespaceManagers>();
                var subscriptionCreator = b.Build<ICreateSubscriptions>();
                var queueCreator = b.Build<ICreateQueues>();
                var topicCreator = b.Build<ICreateTopics>();
                var queueClients = b.Build<ICreateQueueClients>();
                var subscriptionClients = b.Build<ICreateSubscriptionClients>();
                var topicClients = b.Build<ICreateTopicClients>();

                // isn't there a better way to call initialize on object creation?
                var topology = new QueueAndTopicByEndpointTopology(config, 
                    messagingFactories, namespaceManagers, 
                    subscriptionCreator,queueCreator, topicCreator, 
                    queueClients, subscriptionClients, topicClients);
                
                topology.Initialize(context.Settings);
                
                return topology;

            }, DependencyLifecycle.SingleInstance);

        }
    }
}