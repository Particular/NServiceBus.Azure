using System;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.Settings;

namespace NServiceBus.Azure
{
    public class AzureQueueNamingConventions
    {
        public static string Apply(dynamic configSection)
        {
            var queueName = Configure.EndpointName;

            if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
            {
                queueName = (string)configSection.QueueName;

                if ((bool) configSection.QueuePerInstance)
                {
                    SettingsHolder.Set("ScaleOut.UseSingleBrokerQueue", true);
                }
            }
            
            if (SettingsHolder.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                queueName = QueueIndividualizer.Individualize(queueName);

            return queueName;
        }
    }
}