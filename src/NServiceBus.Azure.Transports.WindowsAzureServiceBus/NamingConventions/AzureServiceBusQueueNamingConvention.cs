﻿using NServiceBus.Config;
using NServiceBus.Settings;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    
    public static class AzureServiceBusQueueNamingConvention
    {
        public static Func<string, string> Apply = queueName =>
        {
            var configSection = Configure.GetConfigSection<AzureServiceBusQueueConfig>();

            if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
            {
                queueName = configSection.QueueName;

                if (configSection.QueuePerInstance)
                {
                    SettingsHolder.SetDefault("ScaleOut.UseSingleBrokerQueue", false);
                }
            }

            if (queueName.Length >= 283) // 290 - a spot for the "-" & 6 digits for the individualizer
                queueName = new DeterministicGuidBuilder().Build(queueName).ToString();

            if (!SettingsHolder.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                queueName = QueueIndividualizer.Individualize(queueName);

            return queueName;
        };
    }
}