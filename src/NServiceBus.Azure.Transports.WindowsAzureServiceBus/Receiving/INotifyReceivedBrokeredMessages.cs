using System;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    public interface INotifyReceivedBrokeredMessages
    {
        void Start(Address address, Action<BrokeredMessage> tryProcessMessage);

        void Stop();

        Type MessageType { get; set; }
        Address Address { get; set; }
    }
}