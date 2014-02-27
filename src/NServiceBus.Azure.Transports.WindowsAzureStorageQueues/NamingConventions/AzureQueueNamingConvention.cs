﻿using NServiceBus.Config;
using NServiceBus.Settings;

namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;

    public static class AzureQueueNamingConvention
    {
        public static Func<string, string> Apply = queueName =>
        {
            var configSection = NServiceBus.Configure.GetConfigSection<AzureQueueConfig>();

            if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
            {
                queueName = (string) configSection.QueueName;

                if ((bool) configSection.QueuePerInstance)
                {
                    SettingsHolder.SetDefault("ScaleOut.UseSingleBrokerQueue", false);
                }
            }

            if (queueName.Length >= 253) // 260 - a spot for the "." & 6 digits for the individualizer
                queueName = new DeterministicGuidBuilder().Build(queueName).ToString();

            if (!SettingsHolder.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                queueName = QueueIndividualizer.Individualize(queueName);

            return queueName;
        };
    }
}