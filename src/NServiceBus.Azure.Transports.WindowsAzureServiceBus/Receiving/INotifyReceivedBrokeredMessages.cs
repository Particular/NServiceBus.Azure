namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging;

    public interface INotifyReceivedBrokeredMessages
    {
        void Start(Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage);

        void Stop();

        Type MessageType { get; set; }
        string EntityName { get; set; }
        IEnumerable<string> Namespaces { get; set; }
    }
}