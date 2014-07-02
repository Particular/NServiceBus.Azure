
namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Settings;

    public interface ITopology
    {
        //All of the above should be internal, the ideal interface would be something like this

        void Initialize(ReadOnlySettings setting);

        void Create();

        INotifyReceivedBrokeredMessages Subscribe(Type eventType, Address address);
        void Unsubscribe(INotifyReceivedBrokeredMessages notifier);

        INotifyReceivedBrokeredMessages GetReceiver(Address address);

        ISendBrokeredMessages GetSender(Address destination);
        IPublishBrokeredMessages GetPublisher(Address local);
    }
}
