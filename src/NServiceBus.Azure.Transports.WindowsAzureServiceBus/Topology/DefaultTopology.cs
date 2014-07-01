namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Features;

    public class DefaultTopology : Feature
    {
        public DefaultTopology()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var topology = new QueueAndTopicByEndpointTopology(Configure.Instance);
            topology.Configure(context);
            context.Container.RegisterSingleton<ITopology>(topology);
        }
    }
}