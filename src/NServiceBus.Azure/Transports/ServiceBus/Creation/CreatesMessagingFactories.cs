namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using Features;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Azure.Transports.ServiceBus;
    using Support;

    public class CreatesMessagingFactories : ICreateMessagingFactories
    {
        private static readonly Dictionary<string, MessagingFactory> MessagingFactories = new Dictionary<string, MessagingFactory>();

        private static readonly object FactoryLock = new Object();

        public MessagingFactory Create(string potentialConnectionString)
        {
            var connectionstring = potentialConnectionString != RuntimeEnvironment.MachineName
                                     ? potentialConnectionString
                                     : new DeterminesBestConnectionString().Determine();

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