namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.Transports
{
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// 
    /// </summary>
    public interface ICreateQueues
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="queuename"></param>
        /// <param name="namespace"></param>
        /// <returns></returns>
        QueueDescription Create(string queuename, string @namespace);
    }
}