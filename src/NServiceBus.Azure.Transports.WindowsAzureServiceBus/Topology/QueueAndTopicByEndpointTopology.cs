namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Config;
    using Features;

    public class QueueAndTopicByEndpointTopology : ITopology
    {
        readonly Configure config;

        public QueueAndTopicByEndpointTopology(Configure config)
        {
            this.config = config;
        }

        public Func<Type, string, string> QueueNamingConvention {
            get
            {
                return (messagetype, endpointname) =>
                {
                    var queueName = endpointname;

                    var configSection = config != null ? config.Settings.GetConfigSection<AzureServiceBusQueueConfig>() : null;

                    if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
                    {
                        queueName = configSection.QueueName;
                    }

                    if (queueName.Length >= 283) // 290 - a spot for the "-" & 6 digits for the individualizer
                        queueName = new DeterministicGuidBuilder().Build(queueName).ToString();

                    if (config != null && !config.Settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                        queueName = QueueIndividualizer.Individualize(queueName);

                    return queueName;
                };
            }
        }

        public Func<Type, string, string> SubscriptionNamingConvention
        {
            get
            {
                return (messagetype, endpointname) =>
                {
                    var subscriptionName = messagetype != null ? endpointname + "." + messagetype.Name : endpointname;

                    if (subscriptionName.Length >= 50)
                        subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

                    return subscriptionName;
                };
            }
        }
        public Func<Type, string, string> TopicNamingConvention
        {
            get
            {
                return (messagetype, endpointname) =>
                {
                    var name = endpointname;

                    if (name.Length >= 290)
                        name = new DeterministicGuidBuilder().Build(name).ToString();

                    return name;
                };
            }
        }
        public Func<Address, Address> PublisherAddressConvention
        {
            get
            {
                return address => Address.Parse(TopicNamingConvention(null, address.Queue + ".events") + "@" + address.Machine);
            }
        }
        public Func<Address, Address> PublisherAddressConventionForSubscriptions
        {
            get { return PublisherAddressConvention; }
        }
        public Func<Address, Address> QueueAddressConvention
        {
            get
            {
                return address => Address.Parse(QueueNamingConvention(null, address.Queue) + "@" + address.Machine);
            }
        }

        public void Configure(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<AzureServiceBusQueueConfig>() ?? new AzureServiceBusQueueConfig();
            var transportConfig = context.Settings.GetConfigSection<TransportConfig>() ?? new TransportConfig();

            var queuename = QueueNamingConvention(null, config.Settings.EndpointName());
            Address.InitializeLocalAddress(queuename);

            new ContainerConfiguration().Configure(context, configSection, transportConfig);
        }

        
    }
}