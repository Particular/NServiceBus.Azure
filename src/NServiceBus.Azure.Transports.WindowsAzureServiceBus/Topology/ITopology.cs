
namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Features;
    using Settings;

    public interface ITopology
    {
        // for a large part the topology is defined by the naming conventions


        Func<Type, string, string> SubscriptionNamingConvention { get; }


        Func<Address, Address> PublisherAddressConvention { get; }

        Func<Address, Address> PublisherAddressConventionForSubscriptions { get; }

        Func<Address, Address> QueueAddressConvention { get; }
        
        //GuardAgainstSubscriptionReuseAcrossLogicalEndpoints?

        //ServicebusSubscriptionFilterBuilder?

        //GetTopicClientForDestination?


        //All of the above should be internal, the ideal interface would be something like this

        void Initialize(ReadOnlySettings setting);

        void Create();

        INotifyReceivedBrokeredMessages Subscribe(Type eventType, Address address);
        void Unsubscribe(INotifyReceivedBrokeredMessages notifier);

        INotifyReceivedBrokeredMessages GetReceiver();

        ISendBrokeredMessages GetSender();
        IPublishBrokeredMessages GetPublisher();
    }
}
