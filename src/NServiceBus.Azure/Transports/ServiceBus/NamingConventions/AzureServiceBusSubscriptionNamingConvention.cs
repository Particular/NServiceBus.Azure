namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using NServiceBus.Azure;

    public static class AzureServiceBusSubscriptionNamingConvention
    {
        public static Func<Type, string> Apply = eventType =>
        {
            var subscriptionName = eventType != null ? Configure.EndpointName + "." + eventType.Name : Configure.EndpointName;

            if (subscriptionName.Length >= 50)
                subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

            return subscriptionName;
        };
    }
}