namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Config;
    using NServiceBus.Transports;
    using Settings;

    public class AutoCreateTopic : IWantToRunWhenConfigurationIsComplete
    {
        readonly ICreateTopics topicCreator;

        public AutoCreateTopic()
        {
            topicCreator = new AzureServicebusTopicCreator();
        }

        public void Run()
        {
             var selectedTransport = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

            if (selectedTransport is AzureServiceBus)
            {
                try
                {
                    topicCreator.Create(Address.Local);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    // very likely to exist already
                }
            }
        }
    }
}