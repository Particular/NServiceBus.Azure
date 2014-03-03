namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus;

    public interface ICreateNamespaceManagers
    {
        NamespaceManager Create(string serviceBusNamespace);
    }
}