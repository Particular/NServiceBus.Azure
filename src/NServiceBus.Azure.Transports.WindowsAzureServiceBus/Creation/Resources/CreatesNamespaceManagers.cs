namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus;
    using Support;

    public class CreatesNamespaceManagers : ICreateNamespaceManagers
    {
        private static readonly Dictionary<string, NamespaceManager> NamespaceManagers = new Dictionary<string, NamespaceManager>();

        private static readonly object NamespaceLock = new Object();

        public NamespaceManager Create(string potentialConnectionstring)
        {
            var connectionstring = potentialConnectionstring != RuntimeEnvironment.MachineName
                                      ? potentialConnectionstring
                                      : new DeterminesBestConnectionStringForAzureServiceBus().Determine();

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