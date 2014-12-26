namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;

    public interface IManageSubscriptionClientsLifecycle
    {
        TopicClient Get(string topicName, string @namespace);
    }
}