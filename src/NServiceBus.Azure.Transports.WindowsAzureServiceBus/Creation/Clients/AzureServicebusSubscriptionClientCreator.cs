namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServicebusSubscriptionClientCreator : ICreateSubscriptionClients
    {
        readonly Configure config;

        public AzureServicebusSubscriptionClientCreator(Configure config)
        {
            this.config = config;
        }


       

        public SubscriptionClient Create(SubscriptionDescription description, MessagingFactory factory)
        {
            //if (ShouldAutoCreate)
            //{
            //    subscriptionCreator.Create(topic, eventType, subscriptionname);
            //}
            //var factory = createMessagingFactories.Create(topic.Machine);
            
            return factory.CreateSubscriptionClient(description.TopicPath, description.Name, ShouldRetry() ? ReceiveMode.PeekLock : ReceiveMode.ReceiveAndDelete);
        }

        bool ShouldRetry()
        {
            return (bool)config.Settings.Get("Transactions.Enabled");
        }
    }
}