namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using NServiceBus.Azure.Transports.WindowsAzureServiceBus;

    public static class AzureServiceBusTopicNamingConvention
    {
        public static Func<string, string> Create = name =>
        {
            if (name.Length >= 290)
                name = new DeterministicGuidBuilder().Build(name).ToString();

            return name;
        };
    }
}