using NServiceBus.Config;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    
    public static class AzureServiceBusQueueNamingConvention
    {
        public static Func<string, string> Apply = queueName =>
        {
            var config = Configure.Instance; //todo: inject

            var configSection = config.Settings.GetConfigSection<AzureServiceBusQueueConfig>();

            if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
            {
                queueName = configSection.QueueName;

                //if (configSection.QueuePerInstance)
                //{
                //    config.Settings.SetDefault("ScaleOut.UseSingleBrokerQueue", false);
                //}
            }

            if (queueName.Length >= 283) // 290 - a spot for the "-" & 6 digits for the individualizer
                queueName = new DeterministicGuidBuilder().Build(queueName).ToString();

            if (!config.Settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                queueName = QueueIndividualizer.Individualize(queueName);

            return queueName;
        };
    }
}