namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Microsoft.ServiceBus.Messaging;

    public interface ICreateSubscriptions
    {
        SubscriptionDescription Create(string topicName, string @namespace, Type eventType, string subscriptionname, string forwardTo = null);
        void Delete(string topicName, string @namespace, string subscriptionname);
    }
}