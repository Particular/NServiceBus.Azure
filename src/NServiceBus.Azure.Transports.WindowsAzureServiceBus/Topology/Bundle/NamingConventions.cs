namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.Bundle
{
    using System;
    using System.Text.RegularExpressions;
    using Settings;

    static class NamingConventions
    {
        internal static Func<ReadOnlySettings, string, bool, string> QueueNamingConvention
        {
            get
            {
                return (settings, endpointname, doNotIndividualize) =>
                {
                    var queueName = endpointname;

                    queueName = SanitizeEntityName(queueName);

                    if (queueName.Length >= 283) // 290 - a spot for the "-" & 6 digits for the individualizer
                        queueName = new DeterministicGuidBuilder().Build(queueName).ToString();

                    if (!doNotIndividualize && ShouldIndividualize(settings))
                        queueName = QueueIndividualizer.Individualize(queueName);

                    return queueName;
                };
            }
        }

        static string SanitizeEntityName(string queueName)
        {
            //*Entity segments can contain only letters, numbers, periods (.), hyphens (-), and underscores */

            var rgx = new Regex(@"[^a-zA-Z0-9\-._]");
            var n = rgx.Replace(queueName, "");
            return n;
        }

        static bool ShouldIndividualize(ReadOnlySettings settings)
        {
            // if explicitly set in code
            if (settings != null && settings.HasExplicitValue("ScaleOut.UseSingleBrokerQueue"))
                return !settings.Get<bool>("ScaleOut.UseSingleBrokerQueue");

            // if default is set
            if(settings != null && !settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                return !settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue");

            return false;
        }

        internal static Func<ReadOnlySettings, string, string> SubscriptionNamingConvention
        {
            get
            {
                return (settings, endpointname) =>
                {
                    var subscriptionName = endpointname;

                    subscriptionName = SanitizeEntityName(subscriptionName);

                    if (subscriptionName.Length >= 50)
                        subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

                    if (ShouldIndividualize(settings))
                        subscriptionName = QueueIndividualizer.Individualize(subscriptionName);

                    return subscriptionName;
                };
            }
        }

        internal static Func<ReadOnlySettings, string, string> SqlFilterNamingConvention
        {
            get
            {
                return (settings, filtername) =>
                {
                    filtername = SanitizeEntityName(filtername);

                    if (filtername.Length >= 50)
                        filtername = new DeterministicGuidBuilder().Build(filtername).ToString();

                    return filtername;
                };
            }
        }

        internal static Func<ReadOnlySettings, string, int, string> TopicNamingConvention
        {
            get
            {
                return (settings, partitionPrefix, partition) =>
                {
                    var name = partitionPrefix + "-" + partition;

                    name = SanitizeEntityName(name);

                    return name;
                };
            }
        }

    }
}