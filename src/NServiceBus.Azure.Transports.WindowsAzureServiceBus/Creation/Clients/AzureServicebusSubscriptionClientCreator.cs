namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Microsoft.ServiceBus.Messaging;
    using Settings;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServicebusSubscriptionClientCreator : ICreateSubscriptionClients
    {
        readonly ICreateSubscriptions subscriptionCreator;
        readonly ICreateMessagingFactories createMessagingFactories;

        public int MaxRetries { get; set; }

        public AzureServicebusSubscriptionClientCreator(ICreateSubscriptions subscriptionCreator, ICreateMessagingFactories createMessagingFactories)
        {
            this.subscriptionCreator = subscriptionCreator;
            this.createMessagingFactories = createMessagingFactories;
        }


        public SubscriptionClient Create(Address address, Type eventType)
        {
            var subscriptionname = AzureServiceBusSubscriptionNamingConvention.Apply(eventType);

            try
            {
                return Create(eventType, address, subscriptionname);
            }
            catch (SubscriptionAlreadyInUseException)
            {
                // if this occurs, it means that another endpoint is using the same eventtype name but in another namespace,
                // so let's differenatiate including this namespace, odds are very likely that we will get a guid instead
                // that's why we're not defaulting to this convention.

                subscriptionname = AzureServiceBusSubscriptionNamingConvention.ApplyFullNameConvention(eventType);

                return Create(eventType, address, subscriptionname);
            }
            
        }

        public SubscriptionClient Create(Type eventType, Address topic, string subscriptionname)
        {
            if (QueueAutoCreation.ShouldAutoCreate)
            {
                subscriptionCreator.Create(topic, eventType, subscriptionname);
            }
            var factory = createMessagingFactories.Create(topic.Machine);
            return factory.CreateSubscriptionClient(topic.Queue, subscriptionname, ShouldRetry() ? ReceiveMode.PeekLock : ReceiveMode.ReceiveAndDelete);
        }

        public void Delete(Address topic, string subscriptionname)
        {
            if (QueueAutoCreation.ShouldAutoCreate)
            {
                subscriptionCreator.Delete(topic, subscriptionname);
            }
        }

        bool ShouldRetry()
        {
            return (bool)SettingsHolder.Get("Transactions.Enabled");
        }
    }
}