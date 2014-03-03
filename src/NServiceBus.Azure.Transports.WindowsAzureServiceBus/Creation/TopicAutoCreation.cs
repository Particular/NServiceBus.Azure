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

            // to stay backward compat, this used to be autocreated the constructor but is now injected
            // so if this class was manually instantiated, it could lead to a null ref otherwise.
            if (TopicCreator == null)
            {
                TopicCreator = new AzureServicebusTopicCreator(); 
            }

            var selectedTransport = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");
            if (selectedTransport is AzureServiceBus)
            {
                TopicCreator.CreateIfNecessary(AzureServiceBusPublisherAddressConvention.Apply(Address.Local));
            }
        }
    }
}
