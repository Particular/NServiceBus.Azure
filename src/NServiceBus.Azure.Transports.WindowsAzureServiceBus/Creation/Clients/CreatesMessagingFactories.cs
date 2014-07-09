namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging;

    internal class CreatesMessagingFactories : ICreateMessagingFactories
    {
        readonly ICreateNamespaceManagers createNamespaceManagers;

        private static readonly Dictionary<string, MessagingFactory> MessagingFactories = new Dictionary<string, MessagingFactory>();

        private static readonly object FactoryLock = new Object();

        public CreatesMessagingFactories(ICreateNamespaceManagers createNamespaceManagers)
        {
            this.createNamespaceManagers = createNamespaceManagers;
        }

        public MessagingFactory Create(Address address)
        {
            MessagingFactory factory;
            if (!MessagingFactories.TryGetValue(address.ToString(), out factory))
            {
                lock (FactoryLock)
                {
                    if (!MessagingFactories.TryGetValue(address.ToString(), out factory))
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
                        factory = MessagingFactory.Create(namespaceManager.Address, settings);
                        
                        MessagingFactories[address.ToString()] = factory;
                    }
                }
            }
            return factory;
        }
    }
}