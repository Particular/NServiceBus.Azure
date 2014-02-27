namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public static class AzureServiceBusSubscriptionNamingConvention
    {
        public static Func<Type, string> Apply = eventType =>
        {
            var subscriptionName = eventType != null ? Configure.EndpointName + "." + eventType.Name : Configure.EndpointName;

            if (subscriptionName.Length >= 50)
                subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

            return subscriptionName;
        };

        public static Func<Type, string> ApplyFullNameConvention = eventType =>
        {
            var subscriptionName = eventType != null ? Configure.EndpointName + "." + eventType.FullName : Configure.EndpointName;

            if (subscriptionName.Length >= 50)
                subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

            return subscriptionName;
        };
    }
}