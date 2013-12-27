namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public static class AzureServiceBusQueueAddressConvention
    {
        public static Func<Address, Address> Apply = address => Address.Parse(AzureServiceBusQueueNamingConvention.Apply(address.Queue) + "@" + address.Machine);
    }
}