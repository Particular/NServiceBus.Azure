namespace NServiceBus.Features
{
    using Config;
    using Config.ConfigurationSource;

    public class OverrideMaxRetries : IProvideConfiguration<TransportConfig>
    {
        readonly IConfigurationSource source;

        public OverrideMaxRetries(IConfigurationSource source)
        {
            this.source = source;
        }

        public TransportConfig GetConfiguration()
        {
            var c = source.GetConfiguration<AzureServiceBusQueueConfig>();
            var t = source.GetConfiguration<TransportConfig>();
            if (c != null && t != null && t.MaxRetries > c.MaxDeliveryCount - 2)
            {
                t = new TransportConfig()
                {
                    MaximumConcurrencyLevel = t.MaximumConcurrencyLevel,
                    MaxRetries = c.MaxDeliveryCount - 2,
                    MaximumMessageThroughputPerSecond = t.MaximumMessageThroughputPerSecond
                };
            }
            return t;
        }
    }
}