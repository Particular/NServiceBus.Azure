using System;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    public interface INotifyReceivedGatewayMessages
    {
        /// <summary>
        /// 
        /// </summary>
        int ServerWaitTime { get; set; }

        /// <summary>
        /// 
        /// </summary>
        int BatchSize { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        int BackoffTimeInSeconds { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        void Start(string address, Action<BrokeredMessage> tryProcessMessage);
        
        /// <summary>
        /// 
        /// </summary>
        void Stop();
    }
}