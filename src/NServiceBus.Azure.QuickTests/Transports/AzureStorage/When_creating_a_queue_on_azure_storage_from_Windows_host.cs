namespace NServiceBus.Azure.QuickTests.Transports.AzureStorage
{
    using Azure.Transports.WindowsAzureStorageQueues;
    using Microsoft.WindowsAzure.Storage;
    using NUnit.Framework;

    [TestFixture]
    [Category("Azure")]
    public class When_creating_a_queue_on_azure_storage_from_Windows_host
    {
        AzureMessageQueueCreator azureMessageQueueCreator;
        Address address;

        [SetUp]
        public void SetUp()
        {
            azureMessageQueueCreator = new AzureMessageQueueCreator
            {
                Client = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient()
            };
        }

        [TestCase("TestQueue")]
        [TestCase("Test.Queue")]
        [TestCase("TestQueueTestQueueTestQueueTestQueueTestQueueTestQueueTestQueue")]
        public void Should_fix_queue_name_when_upper_case_letters_are_used_dots_or_longer_than_63_charachters(string queueName)
        {
            address = new Address(queueName, "");

            Assert.DoesNotThrow(() => azureMessageQueueCreator.CreateQueueIfNecessary(address, "UseDevelopmentStorage=true"));
        }

        [TestCase("Test_Queue")]
        [TestCase("-TestQueue")]
        [TestCase("TestQueue-")]
        [TestCase("TQ")]
        public void Should_throw_exception_with_enough_information_about_invalid_queue_name(string queueName)
        {
            address = new Address(queueName, "");

            var exception = Assert.Throws<StorageException>(() => azureMessageQueueCreator.CreateQueueIfNecessary(address, "UseDevelopmentStorage=true"));
            Assert.That(exception.Message, Contains.Substring(address.Queue.ToLowerInvariant()));
            Assert.That(exception.Message, Contains.Substring("http://msdn.microsoft.com/en-us/library/windowsazure/dd179349.aspx"));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                azureMessageQueueCreator.Client.GetQueueReference(AzureMessageQueueUtils.GetQueueName(address)).Delete();
            }
            catch
            {
            }
        }
    }
}