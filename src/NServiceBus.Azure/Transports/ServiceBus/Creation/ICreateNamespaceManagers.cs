namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus;

    public interface ICreateNamespaceManagers
    {
        NamespaceManager Create(string serviceBusNamespace);
    }
}