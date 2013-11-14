namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public static class AzureServiceBusPublisherAddressConventionForSubscriptions
    {
        public static Func<Address, string> Create = AzureServiceBusPublisherAddressConvention.Create;
    }
}