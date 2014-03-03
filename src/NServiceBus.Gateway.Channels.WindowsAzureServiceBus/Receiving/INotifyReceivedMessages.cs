using System;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    public interface INotifyReceivedMessages
    {
        void Start(string address, Action<BrokeredMessage> tryProcessMessage);
        void Stop();
    }
}