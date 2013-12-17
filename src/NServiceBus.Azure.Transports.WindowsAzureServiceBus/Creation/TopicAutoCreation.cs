namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Features;
    using Config;
    using NServiceBus.Transports;
    using Settings;

    public class TopicAutoCreation: Feature, IWantToRunWhenConfigurationIsComplete
    {
        readonly ICreateTopics topicCreator;

        public TopicAutoCreation()
        {
            topicCreator = new AzureServicebusTopicCreator();
        }

        public void Run()
        {
            if (!IsEnabled<TopicAutoCreation>())
                return;

            var selectedTransport = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");
            if (selectedTransport is AzureServiceBus)
            {
                topicCreator.CreateIfNecessary(Address.Local);
            }
        }
    }
}