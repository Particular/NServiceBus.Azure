namespace NServiceBus.Features
{
    using System.Reflection;
    using Config;
    using Config.ConfigurationSource;

    public class OverrideMaxRetries : IProvideConfiguration<TransportConfig>
    {
        public TransportConfig GetConfiguration()
        {
            // todo inject configure instance
            var source = Configure.Instance.Settings.Get<IConfigurationSource>();
            
            var c = source.GetConfiguration<AzureServiceBusQueueConfig>();
            var t = source.GetConfiguration<TransportConfig>();

            if (c == null)
            {
                c = new AzureServiceBusQueueConfig();
            }
            if (t == null)
            {
                t = new TransportConfig();
            }

            return new TransportConfig
                        {
                            MaximumConcurrencyLevel = t.MaximumConcurrencyLevel,
                            MaxRetries = t.MaxRetries >= c.MaxDeliveryCount - 1 ? c.MaxDeliveryCount - 2 : t.MaxRetries,
                            MaximumMessageThroughputPerSecond = t.MaximumMessageThroughputPerSecond
                        };
        }
    }
}