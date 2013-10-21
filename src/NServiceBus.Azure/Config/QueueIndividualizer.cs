namespace NServiceBus.Config
{
    using System.Globalization;
    using Microsoft.WindowsAzure.ServiceRuntime;

    internal class QueueIndividualizer
    {
        public static string Individualize(string queueName)
        {
            var parser = new ConnectionStringParser();
            var individualQueueName = queueName;
            if (SafeRoleEnvironment.IsAvailable)
            {
                var index = parser.ParseIndexFrom(RoleEnvironment.CurrentRoleInstance.Id);
                individualQueueName = parser.ParseQueueNameFrom(queueName)
                                          + (index > 0 ? "-" : "")
                                          + (index > 0 ? index.ToString(CultureInfo.InvariantCulture) : "");

                if (queueName.Contains("@"))
                    individualQueueName += "@" + parser.ParseNamespaceFrom(queueName);
            }

            return individualQueueName;
        }
    }
}