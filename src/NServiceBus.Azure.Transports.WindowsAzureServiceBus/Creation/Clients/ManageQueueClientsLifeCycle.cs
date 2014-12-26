namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ServiceBus.Messaging;

    class ManageQueueClientsLifeCycle : IManageQueueClientsLifecycle
    {
        const int numberOfQueueClientsPerAddress = 4;

        ICreateQueueClients queueClientCreator;
        readonly IManageMessagingFactoriesLifecycle messagingFactories;

        ConcurrentDictionary<string, CircularBuffer<QueueClientEntry>> queueClients = new ConcurrentDictionary<string, CircularBuffer<QueueClientEntry>>();

        public ManageQueueClientsLifeCycle(ICreateQueueClients queueClientCreator, IManageMessagingFactoriesLifecycle messagingFactories)
        {
            this.queueClientCreator = queueClientCreator;
            this.messagingFactories = messagingFactories;
        }

        public QueueClient Get(string queueName, string @namespace)
        {
            var key = queueName + "@" + @namespace;
            var buffer = queueClients.GetOrAdd(key, s =>
            {
                var b = new CircularBuffer<QueueClientEntry>(numberOfQueueClientsPerAddress);
                for (var i = 0; i < numberOfQueueClientsPerAddress; i++)
                {
                    var factory = messagingFactories.Get(queueName, @namespace);
                    b.Put(new QueueClientEntry
                    {
                        Client = queueClientCreator.Create(queueName, factory)
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
                        var factory = messagingFactories.Get(queueName, @namespace);
                        entry.Client = queueClientCreator.Create(queueName, factory);
                    }
                }
            }

            return entry.Client;

        }

        class QueueClientEntry
        {
            internal Object mutex = new object();
            internal QueueClient Client;
        }
    }
}