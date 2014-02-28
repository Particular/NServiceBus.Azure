namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public static class AzureServiceBusTopicNamingConvention
    {
        public static Func<string, string> Apply = name =>
        {
            if (name.Length >= 290)
                name = new DeterministicGuidBuilder().Build(name).ToString();

            return name;
        };
    }
}