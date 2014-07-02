namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus;
    using Support;

    internal class CreatesNamespaceManagers : ICreateNamespaceManagers
    {
        readonly Configure config;
        private static readonly Dictionary<string, NamespaceManager> NamespaceManagers = new Dictionary<string, NamespaceManager>();

        private static readonly object NamespaceLock = new Object();

        public CreatesNamespaceManagers(Configure config)
        {
            this.config = config;
        }

        public NamespaceManager Create(string potentialConnectionstring)
        {
            var connectionStringParser = new DeterminesBestConnectionStringForAzureServiceBus();
            var connectionstring = potentialConnectionstring != RuntimeEnvironment.MachineName && connectionStringParser.IsPotentialServiceBusConnectionString(potentialConnectionstring)
                                      ? potentialConnectionstring
                                      : connectionStringParser.Determine(config.Settings);

            NamespaceManager manager;
            if (!NamespaceManagers.TryGetValue(connectionstring, out manager))
            {
                lock (NamespaceLock)
                {
                    if (!NamespaceManagers.TryGetValue(connectionstring, out manager))
                    {
                        manager = NamespaceManager.CreateFromConnectionString(connectionstring);
                        NamespaceManagers[connectionstring] = manager;
                    }
                }
            }
            return manager;
        }
    }
}