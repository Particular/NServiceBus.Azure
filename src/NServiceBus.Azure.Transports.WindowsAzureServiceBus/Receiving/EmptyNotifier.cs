namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging;

    class EmptyNotifier : INotifyReceivedBrokeredMessages
    {
        public void Start(Action<BrokeredMessage> tryProcessMessage, Action<Exception> errorProcessingMessage)
        {
        }

        public void Stop()
        {
        }

        public Type MessageType { get; set; }
        public string EntityName { get; set; }
        public IEnumerable<string> Namespaces { get; set; }
    }
}