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
            TestHelper.PerformStorageCleanup();
        }

        [Test]
        public void Should_retain_timeout_state()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();
            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            timeoutPersister.Add(timeout);

            var peekedTimeout = timeoutPersister.Peek(timeout.Id);

            Assert.AreEqual(timeout.State, peekedTimeout.State);
        }
    }
}