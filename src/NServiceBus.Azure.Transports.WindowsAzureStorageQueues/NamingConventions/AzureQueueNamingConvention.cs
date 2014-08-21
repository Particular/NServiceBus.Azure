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

            if (!settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                queueName = QueueIndividualizer.Individualize(queueName);

            return queueName;
        };
    }
}