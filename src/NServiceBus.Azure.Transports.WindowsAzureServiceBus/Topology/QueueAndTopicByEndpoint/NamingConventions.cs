namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.QueueAndTopicByEndpoint
{
    using System;
    using Config;
    using Settings;

    internal static class NamingConventions
    {
        internal static Func<ReadOnlySettings, Type, string, string> QueueNamingConvention
        {
            get
            {
                return (settings, messagetype, endpointname) =>
                {
                    var queueName = endpointname;

                    var configSection = settings != null ? settings.GetConfigSection<AzureServiceBusQueueConfig>() : null;

                    if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
                    {
                        queueName = configSection.QueueName;
                    }

                    if (queueName.Length >= 283) // 290 - a spot for the "-" & 6 digits for the individualizer
                        queueName = new DeterministicGuidBuilder().Build(queueName).ToString();

                    if(ShouldIndividualize(configSection, settings))
                        queueName = QueueIndividualizer.Individualize(queueName);

                    return queueName;
                };
            }
        }

        static bool ShouldIndividualize(AzureServiceBusQueueConfig configSection, ReadOnlySettings settings)
        {
            // if explicitly set in code
            if (settings != null && settings.HasExplicitValue("ScaleOut.UseSingleBrokerQueue"))
                return !settings.Get<bool>("ScaleOut.UseSingleBrokerQueue");

            // if explicitly set in config
            if (configSection != null)
                return configSection.QueuePerInstance;

            // if default is set
            if(settings != null && !settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                return !settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue");

            return false;
        }

        internal static Func<Configure, Type, string, string> SubscriptionNamingConvention
        {
            get
            {
                return (config, messagetype, endpointname) =>
                {
                    var subscriptionName = messagetype != null ? endpointname + "." + messagetype.Name : endpointname;

                    if (subscriptionName.Length >= 50)
                        subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

                    if(ShouldIndividualize(null, config.Settings))
                        subscriptionName = QueueIndividualizer.Individualize(subscriptionName);

                    return subscriptionName;
                };
            }
        }

        internal static Func<Configure, Type, string, string> SubscriptionFullNamingConvention
        {
            get
            {
                return (config, messagetype, endpointname) =>
                {
                    var subscriptionName = messagetype != null ? endpointname + "." + messagetype.FullName : endpointname;

                    if (subscriptionName.Length >= 50)
                        subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

                    if (ShouldIndividualize(null, config.Settings))
                        subscriptionName = QueueIndividualizer.Individualize(subscriptionName);

                    return subscriptionName;
                };
            }
        }

        internal static Func<Configure, Type, string, string> TopicNamingConvention
        {
            get
            {
                return (config, messagetype, endpointname) =>
                {
                    var name = endpointname;

                    if (name.Length >= 290)
                        name = new DeterministicGuidBuilder().Build(name).ToString();

                    return name;
                };
            }
        }

        internal static Func<Configure, Address, Address> PublisherAddressConvention
        {
            get
            {
                return (config, address) => Address.Parse(TopicNamingConvention(config, null, address.Queue + ".events") + "@" + address.Machine);
            }
        }

        internal static Func<Configure, Address, Address> PublisherAddressConventionForSubscriptions
        {
            get { return PublisherAddressConvention; }
        }

        internal static Func<Configure, Address, Address> QueueAddressConvention
        {
            get
            {
                return (config, address) => Address.Parse(QueueNamingConvention(config.Settings, null, address.Queue) + "@" + address.Machine);
            }
        }
    }
}