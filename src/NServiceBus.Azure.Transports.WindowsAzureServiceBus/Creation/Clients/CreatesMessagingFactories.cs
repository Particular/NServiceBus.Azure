namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ServiceBus.Messaging;

    internal class CreatesMessagingFactories : ICreateMessagingFactories
    {
        ICreateNamespaceManagers createNamespaceManagers;

        private ConcurrentDictionary<string, MessagingFactory> MessagingFactories = new ConcurrentDictionary<string, MessagingFactory>();

        public CreatesMessagingFactories(ICreateNamespaceManagers createNamespaceManagers)
        {
            this.createNamespaceManagers = createNamespaceManagers;
        }

        public MessagingFactory Create(Address address)
        {
            return MessagingFactories.GetOrAdd(address.ToString(), s =>
            {
                var potentialConnectionString = address.Machine;
                var namespaceManager = createNamespaceManagers.Create(potentialConnectionString);

                var settings = new MessagingFactorySettings
                {
                    TokenProvider = namespaceManager.Settings.TokenProvider,
                    NetMessagingTransportSettings =
                    {
                        BatchFlushInterval = TimeSpan.FromSeconds(0.1)
                    }
                };
                return MessagingFactory.Create(namespaceManager.Address, settings);
            });
       
        }
    }
}