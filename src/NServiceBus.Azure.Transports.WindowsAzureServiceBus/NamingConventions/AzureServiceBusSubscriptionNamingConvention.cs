namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;

    public static class AzureServiceBusSubscriptionNamingConvention
    {
        public static Func<Type, string, string> Apply = (eventType, endpointName) =>
        {

            var subscriptionName = eventType != null ? endpointName + "." + eventType.Name : endpointName;

            if (subscriptionName.Length >= 50)
                subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

            return subscriptionName;
        };

        public static Func<Type, string, string> ApplyFullNameConvention = (eventType, endpointName) =>
        {
            var subscriptionName = eventType != null ? endpointName + "." + eventType.FullName : endpointName;

            if (subscriptionName.Length >= 50)
                subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

            return subscriptionName;
        };
    }
}