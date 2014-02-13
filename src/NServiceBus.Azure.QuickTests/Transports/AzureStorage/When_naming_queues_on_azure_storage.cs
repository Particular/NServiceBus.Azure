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
        public void Should_fix_queue_name_when_upper_case_letters_are_used_dots_or_longer_than_63_charachters(string queueName)
        {
            address = new Address(queueName, "UseDevelopmentStorage=true");

            Assert.DoesNotThrow(() => AzureMessageQueueUtils.GetQueueName(address));
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