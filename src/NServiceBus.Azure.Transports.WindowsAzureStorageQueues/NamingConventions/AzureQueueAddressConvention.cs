namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;

    public static class AzureQueueAddressConvention
    {
        public static Func<Address, Address> Apply = address => Address.Parse(AzureQueueNamingConvention.Apply(address.Queue) + "@" + address.Machine);
    }
}