namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.Transports
{
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// 
    /// </summary>
    public interface ICreateTopics
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="namespace"></param>
        /// <returns></returns>
        TopicDescription Create(string topicName, string @namespace);

        TopicDescription Create(string topicPath, NamespaceManager namespaceClient);
    }
}