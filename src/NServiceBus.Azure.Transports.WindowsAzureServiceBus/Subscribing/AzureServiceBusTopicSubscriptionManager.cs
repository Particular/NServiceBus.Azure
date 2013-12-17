using System;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using NServiceBus.Transports;
    using Unicast;
    using Unicast.Transport;

    public class AzureServiceBusTopicSubscriptionManager : IManageSubscriptions
    {
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
            var publisherAddress = AzureServiceBusPublisherAddressConventionForSubscriptions.Apply(original);
            var subscriptionname = AzureServiceBusSubscriptionNamingConvention.Apply(eventType);

            ClientCreator.Create(eventType, publisherAddress, subscriptionname);

            // resolving manually as the bus also gets the subscription manager injected
            // but this is the only way to get to the correct dequeue strategy
            var bus = Configure.Instance.Builder.Build<UnicastBus>();
            var transport = bus.Transport as TransportReceiver;
            if (transport == null) throw new Exception("AzureServiceBusTopicSubscriptionManager can only be used in conjunction with windows azure servicebus, please configure the windows azure servicebus transport");
            var strategy = transport.Receiver as AzureServiceBusDequeueStrategy;
            if (strategy == null) throw new Exception("AzureServiceBusTopicSubscriptionManager can only be used in conjunction with windows azure servicebus, please configure the windows azure servicebus transport");
           
            var notifier = Configure.Instance.Builder.Build<AzureServiceBusSubscriptionNotifier>();
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
            var publisherAddress = AzureServiceBusPublisherAddressConvention.Apply(original);
            var subscriptionname = AzureServiceBusSubscriptionNamingConvention.Apply(eventType);

            ClientCreator.Delete(publisherAddress, subscriptionname);

            // resolving manually as the bus also gets the subscription manager injected
            // but this is the only way to get to the correct dequeue strategy
            var bus = Configure.Instance.Builder.Build<UnicastBus>();
            var transport = bus.Transport as TransportReceiver;
            if (transport == null) throw new Exception("AzureServiceBusTopicSubscriptionManager can only be used in conjunction with windows azure servicebus, please configure the windows azure servicebus transport");
            var strategy = transport.Receiver as AzureServiceBusDequeueStrategy;
            if (strategy == null) throw new Exception("AzureServiceBusTopicSubscriptionManager can only be used in conjunction with windows azure servicebus, please configure the windows azure servicebus transport");
           
            strategy.RemoveNotifier(publisherAddress);
        }
    }
}