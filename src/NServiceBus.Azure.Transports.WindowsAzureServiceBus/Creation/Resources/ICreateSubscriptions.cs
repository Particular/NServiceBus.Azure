namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public interface ICreateSubscriptions
    {
        void Create(Address topic, Type eventType, string subscriptionname);
        void Delete(Address topic, string subscriptionname);
    }
}