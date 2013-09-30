namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus.Messaging;

    public interface ICreateMessagingFactories
    {
        MessagingFactory Create(string serviceBusNamespace);
    }
}