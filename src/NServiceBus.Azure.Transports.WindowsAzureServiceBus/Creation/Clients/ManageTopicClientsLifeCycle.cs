namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ServiceBus.Messaging;

    class ManageTopicClientsLifeCycle : IManageTopicClientsLifecycle
    {
        const int numberOfTopicClientsPerAddress = 4;

        ICreateTopicClients topicClientCreator;
        readonly IManageMessagingFactoriesLifecycle messagingFactories;

        ConcurrentDictionary<string, CircularBuffer<TopicClientEntry>> topicClients = new ConcurrentDictionary<string, CircularBuffer<TopicClientEntry>>();

        public ManageTopicClientsLifeCycle(ICreateTopicClients topicClientCreator, IManageMessagingFactoriesLifecycle messagingFactories)
        {
            this.topicClientCreator = topicClientCreator;
            this.messagingFactories = messagingFactories;
        }

        public TopicClient Get(string topicName, string @namespace)
        {
            var key = topicName + "@" + @namespace;
            var buffer = topicClients.GetOrAdd(key, s =>
            {
                var b = new CircularBuffer<TopicClientEntry>(numberOfTopicClientsPerAddress);
                for (var i = 0; i < numberOfTopicClientsPerAddress; i++)
                {
                    var factory = messagingFactories.Get(topicName, @namespace);
                    b.Put(new TopicClientEntry
                    {
                        Client = topicClientCreator.Create(topicName, factory)
                    });
                }
                return b;
            });

            var entry = buffer.Get();

            if (entry.Client.IsClosed)
            {
                lock (entry.mutex)
                {
                    if (entry.Client.IsClosed)
                    {
                        var factory = messagingFactories.Get(topicName, @namespace);
                        entry.Client = topicClientCreator.Create(topicName, factory);
                    }
                }
            }

            return entry.Client;

        }

        class TopicClientEntry
        {
            internal Object mutex = new object();
            internal TopicClient Client;
        }
    }
}