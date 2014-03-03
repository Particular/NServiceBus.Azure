namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging;

    internal class CreatesMessagingFactories : ICreateMessagingFactories
    {
        private static readonly Dictionary<string, MessagingFactory> MessagingFactories = new Dictionary<string, MessagingFactory>();

        private static readonly object FactoryLock = new Object();

        public MessagingFactory Create(string connectionstring)
        {
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