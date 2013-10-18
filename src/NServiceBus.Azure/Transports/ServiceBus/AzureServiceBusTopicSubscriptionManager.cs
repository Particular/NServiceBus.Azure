using System;
using Microsoft.ServiceBus;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Transport;
    using Transports;

    public class AzureServiceBusTopicSubscriptionManager : IManageSubscriptions
    {
        /// <summary>
        /// 
        /// </summary>
        public NamespaceManager NamespaceClient { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ICreateSubscriptionClients ClientCreator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="original"></param>
        public void Subscribe(Type eventType, Address original)
        {
            var publisherAddress = Address.Parse(AzureServiceBusPublisherAddressConventionForSubscriptions.Create(original));
            var subscriptionname = AzureServiceBusSubscriptionNamingConvention.Apply(eventType);

            ClientCreator.Create(eventType, publisherAddress, subscriptionname);

            // how to make the correct strategy listen to this subscription

            var theBus = Configure.Instance.Builder.Build<UnicastBus>();

            var transport = theBus.Transport as TransportReceiver;

            if (transport == null) return;

            var strategy = transport.Receiver as AzureServiceBusDequeueStrategy;

            if (strategy == null) return;
            
            var notifier = Configure.Instance.Builder.Build<AzureServiceBusTopicNotifier>();
            notifier.EventType = eventType;
            strategy.TrackNotifier(publisherAddress, notifier);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="original"></param>
        public void Unsubscribe(Type eventType, Address original)
        {
            var publisherAddress = Address.Parse(AzureServiceBusPublisherAddressConvention.Create(original));
            var subscriptionname = AzureServiceBusSubscriptionNamingConvention.Apply(eventType);

            if (NamespaceClient.SubscriptionExists(publisherAddress.Queue, subscriptionname))
            {
                NamespaceClient.DeleteSubscription(publisherAddress.Queue, subscriptionname);
            }

            // unhook the listener
        }
    }
}