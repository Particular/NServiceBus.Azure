using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Connect.Channels.WindowsAzureServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreateGatewayQueueClients
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        QueueClient Create(string address);
    }
}
