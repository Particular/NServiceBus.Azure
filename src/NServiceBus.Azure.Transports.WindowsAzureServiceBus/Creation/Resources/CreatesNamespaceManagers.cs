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
            NamespaceManager manager;
            if (!NamespaceManagers.TryGetValue(potentialConnectionstring, out manager))
            {
                lock (NamespaceLock)
                {
                    var connectionStringParser = new DeterminesBestConnectionStringForAzureServiceBus();
                    var connectionstring = potentialConnectionstring != RuntimeEnvironment.MachineName && connectionStringParser.IsPotentialServiceBusConnectionString(potentialConnectionstring)
                                              ? potentialConnectionstring
                                              : connectionStringParser.Determine(config.Settings);
                    
                    if (!NamespaceManagers.TryGetValue(potentialConnectionstring, out manager))
                    {
                        manager = NamespaceManager.CreateFromConnectionString(connectionstring);
                        NamespaceManagers[potentialConnectionstring] = manager;
                    }
                }
            }
            return manager;
        }
    }
}