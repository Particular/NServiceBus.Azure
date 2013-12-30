namespace NServiceBus.AcceptanceTests
{
    using System;
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
            var namespaceManager = NamespaceManager.CreateFromConnectionString(System.Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString"));

            
            var queues = namespaceManager.GetQueues();
            foreach (var queue in queues)
            {
                try
                {
                    namespaceManager.DeleteQueue(queue.Path);
                }
                catch (TimeoutException)
                {
                    // do not let the test fail becayse we couldn't clean up
                }
                
            }

            var topics = namespaceManager.GetTopics();
            foreach (var topic in topics)
            {
                try
                {
                    namespaceManager.DeleteTopic(topic.Path);
                }
                catch (TimeoutException)
                {
                    // do not let the test fail becayse we couldn't clean up
                }
            }
        }
    }
}