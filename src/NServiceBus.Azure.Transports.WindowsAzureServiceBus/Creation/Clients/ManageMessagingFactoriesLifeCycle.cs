namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System.Collections.Concurrent;
    using Microsoft.ServiceBus.Messaging;

    class ManageMessagingFactoriesLifeCycle : IManageMessagingFactoriesLifecycle
    {
        ICreateMessagingFactories createMessagingFactories;
        ConcurrentDictionary<string, MessagingFactory> MessagingFactories = new ConcurrentDictionary<string, MessagingFactory>();

        public ManageMessagingFactoriesLifeCycle(ICreateMessagingFactories createMessagingFactories)
        {
            this.createMessagingFactories = createMessagingFactories;
        }

        public MessagingFactory Get(Address address)
        {
            var key = address.ToString();
            var factory = MessagingFactories.GetOrAdd(key, s => createMessagingFactories.Create(address));

            if (factory.IsClosed)
            {
                var newFactory = createMessagingFactories.Create(address);
                MessagingFactories.TryUpdate(key, newFactory, factory);
                MessagingFactories.TryGetValue(key, out factory);
            }

            return factory;

        }
    }
}