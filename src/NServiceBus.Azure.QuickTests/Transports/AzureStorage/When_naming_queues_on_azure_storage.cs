namespace NServiceBus.Azure.QuickTests.Transports.AzureStorage
{
    using System.Configuration;
    using Azure.Transports.WindowsAzureStorageQueues;
    using NUnit.Framework;

    [TestFixture]
    [Category("Azure")]
    public class When_naming_queues_on_azure_storage
    {
        Address address;

        [TestCase("TestQueue")]
        [TestCase("Test.Queue")]
        [TestCase("TestQueueTestQueueTestQueueTestQueueTestQueueTestQueueTestQueue")]
        [TestCase("Test1234Queue")]
        public void Should_fix_queue_name_when_upper_case_letters_are_used_dots_or_longer_than_63_characters(string queueName)
        {
            address = new Address(queueName, "UseDevelopmentStorage=true");

            Assert.DoesNotThrow(() => AzureMessageQueueUtils.GetQueueName(address));
        }

        [TestCase("Test_Queue")]
        [TestCase("-TestQueue")]
        [TestCase("TestQueue-")]
        [TestCase("TQ")]
        public void Should_fix_queue_name_when_invalid(string queueName)
        {
            address = new Address(queueName, "UseDevelopmentStorage=true");

            Assert.DoesNotThrow(() => AzureMessageQueueUtils.GetQueueName(address));
        }

    }
}