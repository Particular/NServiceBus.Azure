namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Customization;
    using Microsoft.ServiceBus;
    using NUnit.Framework;

    /// <summary>
    /// Base class for all the NSB test that sets up our conventions
    /// </summary>
    [TestFixture]
// ReSharper disable once PartialTypeWithSinglePart
    public abstract partial class NServiceBusAcceptanceTest
    {
        [TearDown]
        public void TearDown()
        {
           // var endpoint = Conventions.EndpointNamingConvention(GetType()).ToLower();

            var namespaceManager = NamespaceManager.CreateFromConnectionString(System.Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString"));

            var queues = namespaceManager.GetQueues(
                //TODO: figure out what queues have been created by the scenario that just ran
                //string.Format("startswith(path, '{0}') eq true", endpoint)
                );
            foreach (var queue in queues)
            {
                namespaceManager.DeleteQueue(queue.Path);
            }

            var topics = namespaceManager.GetTopics(
                //TODO: figure out what queues have been created by the scenario that just ran
                //string.Format("startswith(path, '{0}') eq true", endpoint)
                );
            foreach (var topic in topics)
            {
                namespaceManager.DeleteTopic(topic.Path);
            }
        }
    }
}