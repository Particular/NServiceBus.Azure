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

        [TestCase("TestQueue", "testqueue")]
        [TestCase("Test.Queue", "test-queue")]
        [TestCase("Test1234Queue", "test1234queue")]
        [TestCase("TestQueueTestQueueTestQueueTestQueueTestQueueTestQueueTestQueueTestQueue", "testqueuetestqueuetestqueu-7565a1c3-1977-44ef-1a56-65be23eb5232")]
        public void Should_fix_queue_name_when_upper_case_letters_are_used_dots_or_longer_than_63_charachters(string queueName, string expectedQueueName)
        {
            address = new Address(queueName, "UseDevelopmentStorage=true");

            Assert.AreEqual(expectedQueueName, AzureMessageQueueUtils.GetQueueName(address));
        }

        [TestCase("Test_Queue")]
        [TestCase("-TestQueue")]
        [TestCase("TestQueue-")]
        [TestCase("TQ")]
        public void Should_throw_exception_with_enough_information_about_invalid_queue_name(string queueName)
        {
            address = new Address(queueName, "UseDevelopmentStorage=true");

            var exception = Assert.Throws<ConfigurationErrorsException>(() => AzureMessageQueueUtils.GetQueueName(address));
            Assert.That(exception.Message, Contains.Substring(address.Queue.ToLowerInvariant()));
            Assert.That(exception.Message, Contains.Substring("http://msdn.microsoft.com/en-us/library/windowsazure/dd179349.aspx"));
        }

    }
}