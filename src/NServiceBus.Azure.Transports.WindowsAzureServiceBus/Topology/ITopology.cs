
namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Features;

    public interface ITopology
    {
        // for a large part the topology is defined by the naming conventions

        Func<Type, string, string> QueueNamingConvention { get; }

        Func<Type, string, string> SubscriptionNamingConvention { get; }

        Func<Type, string, string> TopicNamingConvention { get; }

        Func<Address, Address> PublisherAddressConvention { get; }

        Func<Address, Address> PublisherAddressConventionForSubscriptions { get; }

        Func<Address, Address> QueueAddressConvention { get; }
        
        // the container registrations are also dependent on it
        void Configure(FeatureConfigurationContext context);

        //GuardAgainstSubscriptionReuseAcrossLogicalEndpoints?

        //ServicebusSubscriptionFilterBuilder?

        //GetTopicClientForDestination?
    }
}
