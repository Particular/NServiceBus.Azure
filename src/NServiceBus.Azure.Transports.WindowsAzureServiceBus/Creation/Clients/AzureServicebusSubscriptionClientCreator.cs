namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServicebusSubscriptionClientCreator : ICreateSubscriptionClients
    {
        readonly ICreateSubscriptions subscriptionCreator;
        readonly ICreateMessagingFactories createMessagingFactories;
        readonly Configure config;
        readonly ITopology topology;

        public int MaxRetries { get; set; }
        public bool ShouldAutoCreate { get; set; }

        public AzureServicebusSubscriptionClientCreator(ICreateSubscriptions subscriptionCreator, ICreateMessagingFactories createMessagingFactories, Configure config, ITopology topology)
        {
            this.subscriptionCreator = subscriptionCreator;
            this.createMessagingFactories = createMessagingFactories;
            this.config = config;
            this.topology = topology;
        }


        public SubscriptionClient Create(Address address, Type eventType)
        {
            var subscriptionname = topology.SubscriptionNamingConvention(eventType, config.Settings.EndpointName());

            try
            {
                return Create(eventType, address, subscriptionname);
            }
            catch (SubscriptionAlreadyInUseException)
            {
                // if this occurs, it means that another endpoint is using the same eventtype name but in another namespace,
                // so let's differenatiate including this namespace, odds are very likely that we will get a guid instead
                // that's why we're not defaulting to this convention.

                subscriptionname = topology.SubscriptionNamingConvention(eventType, config.Settings.EndpointName());

                return Create(eventType, address, subscriptionname);
            }
            
        }

        public SubscriptionClient Create(Type eventType, Address topic, string subscriptionname)
        {
            if (ShouldAutoCreate)
            {
                subscriptionCreator.Create(topic, eventType, subscriptionname);
            }
            var factory = createMessagingFactories.Create(topic.Machine);
            return factory.CreateSubscriptionClient(topic.Queue, subscriptionname, ShouldRetry() ? ReceiveMode.PeekLock : ReceiveMode.ReceiveAndDelete);
        }

        public void Delete(Address topic, string subscriptionname)
        {
            if (ShouldAutoCreate)
            {
                subscriptionCreator.Delete(topic, subscriptionname);
            }
        }

        bool ShouldRetry()
        {
            return (bool)config.Settings.Get("Transactions.Enabled");
        }
    }
}