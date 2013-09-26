namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Config;

    public class AutoCreateTopic : IWantToRunWhenConfigurationIsComplete
    {
        ICreateTopicClients TopicCreator { get; set; }

        public void Run()
        {
            try
            {
                TopicCreator.CreateTopic(Address.Local);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // very likely to exist already
            }
        }
    }
}