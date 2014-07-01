namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Features;

    public class DefaultTopology : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<QueueAndTopicByEndpointTopology>(DependencyLifecycle.SingleInstance);
        }
    }
}