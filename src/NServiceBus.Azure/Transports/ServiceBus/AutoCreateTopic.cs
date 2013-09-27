namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Config;

    public class AutoCreateTopic : IWantToRunWhenConfigurationIsComplete
    {
        readonly ICreateTopics topicCreator;

        public AutoCreateTopic(ICreateTopics topicCreator)
        {
            this.topicCreator = topicCreator;
        }

        public void Run()
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