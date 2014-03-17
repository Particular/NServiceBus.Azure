namespace NServiceBus.Connect.Channels.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;

    public interface ICreateMessagingFactories
    {
        MessagingFactory Create(string serviceBusNamespace);
    }
}