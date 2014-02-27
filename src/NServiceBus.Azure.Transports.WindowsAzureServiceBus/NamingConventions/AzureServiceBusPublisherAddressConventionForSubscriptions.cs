namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public static class AzureServiceBusPublisherAddressConventionForSubscriptions
    {
        public static Func<Address, Address> Apply = AzureServiceBusPublisherAddressConvention.Apply;
    }
}