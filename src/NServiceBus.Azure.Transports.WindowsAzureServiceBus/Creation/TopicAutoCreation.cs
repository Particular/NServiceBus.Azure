namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Features;
    using Config;
    using NServiceBus.Transports;
    using Settings;

    public class TopicAutoCreation: IWantToRunWhenConfigurationIsComplete
    {
        readonly ICreateTopics topicCreator;

        public TopicAutoCreation()
        {
            topicCreator = new AzureServicebusTopicCreator();
        }

        public void Run()
        {
            if (!QueueAutoCreation.ShouldAutoCreate)
                return;

            var selectedTransport = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");
            if (selectedTransport is AzureServiceBus)
            {
                topicCreator.CreateIfNecessary(AzureServiceBusPublisherAddressConvention.Apply(Address.Local));
            }
        }
    }
}