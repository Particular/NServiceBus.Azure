namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ServiceBus;
    using Support;

    internal class CreatesNamespaceManagers : ICreateNamespaceManagers
    {
        readonly Configure config;

        private readonly ConcurrentDictionary<string, NamespaceManager> NamespaceManagers = new ConcurrentDictionary<string, NamespaceManager>();

        public CreatesNamespaceManagers(Configure config)
        {
            this.config = config;
        }

        public NamespaceManager Create(string potentialConnectionstring)
        {
            return NamespaceManagers.GetOrAdd(potentialConnectionstring, s =>
            {
                var connectionStringParser = new DeterminesBestConnectionStringForAzureServiceBus();
                var connectionstring = s != RuntimeEnvironment.MachineName && connectionStringParser.IsPotentialServiceBusConnectionString(s)
                    ? s
                    : connectionStringParser.Determine(config.Settings);

               return NamespaceManager.CreateFromConnectionString(connectionstring);
            });
       
        }
    }
}