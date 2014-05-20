namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public static class AzureServiceBusSubscriptionNamingConvention
    {
        public static Func<Type, string> Apply = eventType =>
        {
            var config = Configure.Instance;

            var subscriptionName = eventType != null ? config.EndpointName + "." + eventType.Name : config.EndpointName;

            if (subscriptionName.Length >= 50)
                subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

            return subscriptionName;
        };

        public static Func<Type, string> ApplyFullNameConvention = eventType =>
        {
            var config = Configure.Instance;

            var subscriptionName = eventType != null ? config.EndpointName + "." + eventType.FullName : config.EndpointName;

            if (subscriptionName.Length >= 50)
                subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

            return subscriptionName;
        };
    }
}