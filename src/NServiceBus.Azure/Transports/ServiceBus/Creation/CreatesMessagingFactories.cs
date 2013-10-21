namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Azure.Transports.ServiceBus;
    
    public class CreatesMessagingFactories : ICreateMessagingFactories
    {
        private static readonly Dictionary<string, MessagingFactory> MessagingFactories = new Dictionary<string, MessagingFactory>();

        private static readonly object FactoryLock = new Object();

        public MessagingFactory Create(string potentialConnectionString)
        {
            var validation = new DeterminesBestConnectionStringForAzureServiceBus();
            var connectionstring = validation.IsPotentialServiceBusConnectionString(potentialConnectionString)
                                     ? potentialConnectionString
                                     : validation.Determine();

            MessagingFactory factory;
            if (!MessagingFactories.TryGetValue(connectionstring, out factory))
            {
                lock (FactoryLock)
                {
                    if (!MessagingFactories.TryGetValue(connectionstring, out factory))
                    {
                        factory = MessagingFactory.CreateFromConnectionString(connectionstring);
                        MessagingFactories[connectionstring] = factory;
                    }
                }
            }
            return factory;
        }
    }
}