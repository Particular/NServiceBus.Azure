namespace NServiceBus.Azure.Tests.DataBus
{
    using System;
    using Config;
    using global::NServiceBus.DataBus;
    using NUnit.Framework;

    [TestFixture]
    [Category("Azure")]
    public class When_using_AzureDataBusGuard
    {
        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Should_not_allow_negative_maximum_retries()
        {
            AzureDataBusGuard.CheckMaxRetries(-1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Should_not_allow_negative_backoff_interval()
        {
            AzureDataBusGuard.CheckBackOffInterval(-1);
        }

        [TestCase(0)]
        [TestCase(AzureDataBusDefaults.DefaultBlockSize + 1)]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Should_not_allow_block_size_more_than_4MB_or_less_than_one_byte(int blockSize)
        {
            AzureDataBusGuard.CheckBlockSize(blockSize);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Should_not_allow_invalid_number_of_threads()
        {
            AzureDataBusGuard.CheckNumberOfIOThreads(0);
        }

        [TestCase("")]
        [TestCase(null)]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_not_allow_invalid_connection_string(string connectionString)
        {
            AzureDataBusGuard.CheckConnectionString(connectionString);
        }

        [TestCase("")]
        [TestCase(null)]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_not_allow_invalid_container_name(string containerName)
        {
            AzureDataBusGuard.CheckContainerName(containerName);
        }

        [TestCase(null)]
        [TestCase(" ")]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_not_allow_null_or_whitespace_base_path(string basePath)
        {
            AzureDataBusGuard.CheckBasePath(basePath);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Should_not_allow_negative_default_time_to_live()
        {
            AzureDataBusGuard.CheckDefaultTTL(-1L);
        }

        [Test]
        public void Should_validate_all_default_settings_for_azure_databus_config()
        {
// ReSharper disable once ObjectCreationAsStatement
            new AzureDataBusConfig();
        }
    }
}
