namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public static class AzureServiceBusPublisherAddressConvention
    {
        public static Func<Address, string> Create = address => AzureServiceBusTopicNamingConvention.Create(address.Queue + ".events");
    }
}