using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreateSubscriptionClients
    {
        SubscriptionClient Create(SubscriptionDescription description, MessagingFactory factory);
    }
}