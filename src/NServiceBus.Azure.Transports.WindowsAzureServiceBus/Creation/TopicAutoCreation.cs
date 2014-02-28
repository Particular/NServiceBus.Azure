namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Features;
    using Config;
    using NServiceBus.Transports;
    using Settings;

    public class TopicAutoCreation : IWantToRunWhenConfigurationIsComplete
    {
        public TopicAutoCreation()
        {
        }

        public ICreateTopics TopicCreator { get; set; }
        
        public void Run()
        {
            if (!QueueAutoCreation.ShouldAutoCreate)
                return;

            var selectedTransport = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");
            if (selectedTransport is AzureServiceBus)
            {
                TopicCreator.CreateIfNecessary(AzureServiceBusPublisherAddressConvention.Apply(Address.Local));
            }
        }
    }
}
