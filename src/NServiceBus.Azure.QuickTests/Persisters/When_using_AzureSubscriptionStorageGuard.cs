namespace NServiceBus.Azure.Tests.Persisters
{
    using System;
    using Config;
    using Subscriptions;
    using NUnit.Framework;

    [TestFixture]
    [Category("Azure")]
    public class When_using_AzureSubscriptionStorageGuard
    {
        [TestCase("")]
        [TestCase(null)]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_not_allow_invalid_connection_string(string connectionString)
        {
            AzureSubscriptionStorageGuard.CheckConnectionString(connectionString);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("1table")]
        [TestCase("aa")]
// ReSharper disable StringLiteralTypo
        [TestCase("aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeffffffffffgggg")] // 
// ReSharper restore StringLiteralTypo
        [ExpectedException(typeof(ArgumentException))]
        public void Should_not_allow_invalid_table_name(string tableName)
        {
            AzureSubscriptionStorageGuard.CheckTableName(tableName);
        }

        [Test]
        public void Should_validate_all_default_settings_for_a_new_config()
        {
            var config = new AzureSubscriptionStorageConfig();
            Assert.AreEqual(AzureSubscriptionStorageDefaults.ConnectionString, config.ConnectionString);
            Assert.AreEqual(AzureSubscriptionStorageDefaults.CreateSchema, config.CreateSchema);
            Assert.AreEqual(AzureSubscriptionStorageDefaults.TableName, config.TableName);
        }
    }
}
