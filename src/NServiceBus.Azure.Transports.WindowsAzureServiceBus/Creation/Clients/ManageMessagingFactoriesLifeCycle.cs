namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ServiceBus.Messaging;

    class ManageMessagingFactoriesLifeCycle : IManageMessagingFactoriesLifecycle
    {
        const int numberOfFactoriesPerAddress = 4;

        ICreateMessagingFactories createMessagingFactories;
        ConcurrentDictionary<string, CircularBuffer<FactoryEntry>> MessagingFactories = new ConcurrentDictionary<string, CircularBuffer<FactoryEntry>>();

        public ManageMessagingFactoriesLifeCycle(ICreateMessagingFactories createMessagingFactories)
        {
            this.createMessagingFactories = createMessagingFactories;
        }

        public MessagingFactory Get(string entityName, string @namespace)
        {
            var key = entityName + "@" + @namespace;
            var buffer = MessagingFactories.GetOrAdd(key, s => {
                var b = new CircularBuffer<FactoryEntry>(numberOfFactoriesPerAddress);
                for(var i = 0; i < numberOfFactoriesPerAddress; i++)
                    b.Put(new FactoryEntry { Factory = createMessagingFactories.Create(@namespace) });
                return b;
            });

            var entry = buffer.Get();

            if (entry.Factory.IsClosed)
            {
                lock (entry.mutex)
                {
                    if (entry.Factory.IsClosed)
                    {
                        entry.Factory = createMessagingFactories.Create(@namespace);
                    }
                }
            }

            return entry.Factory;

        }

        class FactoryEntry
        {
            internal Object mutex = new object();
            internal MessagingFactory Factory;
        }
    }
}