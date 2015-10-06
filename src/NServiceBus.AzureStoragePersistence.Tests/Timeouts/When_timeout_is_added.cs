namespace NServiceBus.AzureStoragePersistence.Tests.Timeouts
{
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_timeout_is_added
    {
        [SetUp]
        public void Perform_storage_cleanup()
        {
            Test_Helper.Perform_Storage_Cleanup();
        }

        [Test]
        public void Should_retain_timeout_state()
        {
            var timeoutPersister = Test_Helper.Create_TimeoutPersister();
            var timeout = Test_Helper.Generate_Timeout_With_Headers();
            timeoutPersister.Add(timeout);

            var peekedTimeout = timeoutPersister.Peek(timeout.Id);

            Assert.AreEqual(timeout.State, peekedTimeout.State);
        }
    }
}