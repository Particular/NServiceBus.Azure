namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Features;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServicebusSubscriptionClientCreator : ICreateSubscriptionClients
    {
        readonly ICreateSubscriptions subscriptionCreator;
        readonly ICreateMessagingFactories createMessagingFactories;

        public AzureServicebusSubscriptionClientCreator(ICreateSubscriptions subscriptionCreator, ICreateMessagingFactories createMessagingFactories)
        {
            this.subscriptionCreator = subscriptionCreator;
            this.createMessagingFactories = createMessagingFactories;
        }


        public SubscriptionClient Create(Address address, Type eventType)
        {
            var subscriptionname = AzureServiceBusSubscriptionNamingConvention.Apply(eventType);
            return Create(eventType, address, subscriptionname);
        }

        public SubscriptionClient Create(Type eventType, Address topic, string subscriptionname)
        {
            if (Feature.IsEnabled<TopicAutoCreation>())
            {
                subscriptionCreator.Create(topic, eventType, subscriptionname);
            }
            var factory = createMessagingFactories.Create(topic.Machine);
            return factory.CreateSubscriptionClient(topic.Queue, subscriptionname, ReceiveMode.PeekLock);
        }

        public void Delete(Address topic, string subscriptionname)
        {
            if (Feature.IsEnabled<TopicAutoCreation>())
            {
                subscriptionCreator.Delete(topic, subscriptionname);
            }
        }
    }
}