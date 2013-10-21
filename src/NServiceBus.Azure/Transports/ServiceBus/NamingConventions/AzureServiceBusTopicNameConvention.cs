namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using NServiceBus.Azure;

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