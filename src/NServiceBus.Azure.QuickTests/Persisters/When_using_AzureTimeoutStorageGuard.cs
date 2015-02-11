namespace NServiceBus.Azure.Tests.Persisters
{
    using System;
    using NServiceBus.Config;
    using NServiceBus.Timeout;
    using NUnit.Framework;

    public class When_using_AzureTimeoutStorageGuard
    {
        [TestCase("")]
        [TestCase(null)]
        public void Should_not_allow_invalid_connection_string(string connectionString)
        {
            Assert.Throws<ArgumentException>(() => AzureTimeoutStorageGuard.CheckConnectionString(connectionString));
        }

        [Test]
        public void Should_not_allow_catch_up_interval_less_than_1_second()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AzureTimeoutStorageGuard.CheckCatchUpInterval(0));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("1table")]
        [TestCase("aa")]
        // ReSharper disable StringLiteralTypo
        [TestCase("aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeffffffffffgggg")] // 
        // ReSharper restore StringLiteralTypo
        public void Should_not_allow_invalid_table_name(string tableName)
        {
            Assert.Throws<ArgumentException>(() => AzureTimeoutStorageGuard.CheckTableName(tableName));
        }

        [TestCase("")]
        [TestCase("invalid:key")]
        public void Should_not_allow_invalid_partition_key_scope(string partitionKeyScope)
        {
            Assert.Throws<ArgumentException>(() => AzureTimeoutStorageGuard.CheckPartitionKeyScope(partitionKeyScope));
        }

        [Test]
        public void Should_validate_all_default_settings_for_a_new_config()
        {
            var config = new AzureTimeoutPersisterConfig();
            Assert.AreEqual(AzureTimeoutStorageDefaults.ConnectionString, config.ConnectionString);
            Assert.AreEqual(AzureTimeoutStorageDefaults.TimeoutManagerDataTableName, config.TimeoutManagerDataTableName);
            Assert.AreEqual(AzureTimeoutStorageDefaults.TimeoutDataTableName, config.TimeoutDataTableName);
            Assert.AreEqual(AzureTimeoutStorageDefaults.PartitionKeyScope, config.PartitionKeyScope);
            Assert.AreEqual(AzureTimeoutStorageDefaults.CatchUpInterval, config.CatchUpInterval);
            Assert.AreEqual(AzureTimeoutStorageDefaults.CreateSchema, config.CreateSchema);
        }
    }
}