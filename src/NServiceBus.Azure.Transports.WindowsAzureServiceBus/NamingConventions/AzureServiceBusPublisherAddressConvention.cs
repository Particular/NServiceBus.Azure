namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public static class AzureServiceBusPublisherAddressConvention
    {
        public static Func<Address, Address> Apply = address => Address.Parse(AzureServiceBusTopicNamingConvention.Apply(address.Queue + ".events") + "@" + address.Machine);
    }
}