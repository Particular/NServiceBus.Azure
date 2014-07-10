namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public interface ISendBrokeredMessages
    {
        Task Send(BrokeredMessage brokeredMessage);
    }
}