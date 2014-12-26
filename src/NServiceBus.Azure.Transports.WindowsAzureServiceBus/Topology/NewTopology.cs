namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using NServiceBus.Azure.Transports.WindowsAzureServiceBus.Bundle;
    using NServiceBus.Azure.Transports.WindowsAzureServiceBus.Transports;
    using NServiceBus.Config;
    using NServiceBus.Features;

    public class NewTopology : Feature
    {

        protected override void Setup(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<AzureServiceBusQueueConfig>() ?? new AzureServiceBusQueueConfig();

            new ContainerConfiguration().Configure(context, configSection);

            context.Container.ConfigureComponent(b =>
            {
                var config = b.Build<Configure>();
                var messagingFactories = b.Build<IManageMessagingFactoriesLifecycle>();
                var subscriptionCreator = b.Build<ICreateSubscriptions>();
                var queueCreator = b.Build<ICreateQueues>();
                var topicCreator = b.Build<ICreateTopics>();
                var queueClients = b.Build<IManageQueueClientsLifecycle>();
                var subscriptionClients = b.Build<ICreateSubscriptionClients>();
                var topicClients = b.Build<IManageTopicClientsLifecycle>();
                var queueClientCreator = b.Build<ICreateQueueClients>();

                // isn't there a better way to call initialize on object creation?
                var topology = new BundleTopology(config,
                    messagingFactories,
                    subscriptionCreator, queueCreator, topicCreator,
                    queueClients, subscriptionClients, topicClients, queueClientCreator);

                topology.Initialize(context.Settings);

                return topology;

            }, DependencyLifecycle.SingleInstance);

        }
    }
}