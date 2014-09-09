using NServiceBus.Config;

namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;
    using Settings;

    public static class AzureQueueNamingConvention
    {
        public static Func<ReadOnlySettings, string> Apply = settings =>
        {
            var configSection = settings.GetConfigSection<AzureQueueConfig>();
            var queueName = settings.EndpointName();

            if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
            {
                queueName = configSection.QueueName;
            }

            if (queueName.Length >= 253) // 260 - a spot for the "." & 6 digits for the individualizer
                queueName = new DeterministicGuidBuilder().Build(queueName).ToString();

            if (ShouldIndividualize(configSection, settings))
                queueName = QueueIndividualizer.Individualize(queueName);

            return queueName;
        };

        static bool ShouldIndividualize(AzureQueueConfig configSection, ReadOnlySettings settings)
        {
            // if explicitly set in code
            if (settings != null && settings.HasExplicitValue("ScaleOut.UseSingleBrokerQueue"))
                return !settings.Get<bool>("ScaleOut.UseSingleBrokerQueue");

            // if explicitly set in config
            if (configSection != null)
                return configSection.QueuePerInstance;

            // if default is set
            if (settings != null && !settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                return !settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue");

            return false;
        }
    }
}