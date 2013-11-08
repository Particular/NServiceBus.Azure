using System;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    public interface INotifyReceivedMessages
    {
        void Start(Address address, Action<BrokeredMessage> tryProcessMessage);
        void Stop();
    }
}