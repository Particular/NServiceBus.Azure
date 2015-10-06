namespace NServiceBus.AzureStoragePersistence.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.Azure;
    using NServiceBus.Config;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_removing_timeouts_from_the_storage
    {

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            Test_Helper.Remove_All_Blobs();
        }

        [Test]
        public void Should_return_correct_headers_when_timeout_is_TryRemoved()
        {
            var timeoutPersister = Test_Helper.Create_TimeoutPersister();

            var timeout = Test_Helper.Generate_Timeout_With_Headers();
            timeoutPersister.Add(timeout);

            var timeouts = Test_Helper.Get_All_Timeouts(timeoutPersister);

            Assert.True(timeouts.Count == 1);

            TimeoutData timeoutData;
            timeoutPersister.TryRemove(timeouts.First().Item1, out timeoutData);

            CollectionAssert.AreEqual(new Dictionary<string, string> { { "Prop1", "1234" }, { "Prop2", "text" } }, timeoutData.Headers);

            Test_Helper.Cleanup_Storage_Account(timeoutData, timeoutPersister);
        }


        [Test]
        public void Should_return_orrect_headers_when_timeout_is_Peeked()
        {
            var timeoutPersister = Test_Helper.Create_TimeoutPersister();

            var timeout = Test_Helper.Generate_Timeout_With_Headers();
            timeoutPersister.Add(timeout);

            var timeouts = Test_Helper.Get_All_Timeouts(timeoutPersister);

            Assert.True(timeouts.Count == 1);

            var timeoutId = timeouts.First().Item1;
            var timeoutData = timeoutPersister.Peek(timeoutId);

            CollectionAssert.AreEqual(new Dictionary<string, string> { { "Prop1", "1234" }, { "Prop2", "text" } }, timeoutData.Headers);

            Test_Helper.Cleanup_Storage_Account(timeout, timeoutPersister);
        }

        [Test]
        public void Peek_should_return_null_for_non_existing_timeout()
        {
            var timeoutPersister = Test_Helper.Create_TimeoutPersister();

            var timeoutData = timeoutPersister.Peek("A2B34534324F3435A324234C");

            Assert.IsNull(timeoutData);
        }

        [Test]
        public void Should_remove_timeouts_by_id_using_old_interface()
        {
            var timeoutPersister = Test_Helper.Create_TimeoutPersister();
            var timeout1 = Test_Helper.Generate_Timeout_With_Headers();
            var timeout2 = Test_Helper.Generate_Timeout_With_Headers();
            timeoutPersister.Add(timeout1);
            timeoutPersister.Add(timeout2);

            var timeouts = Test_Helper.Get_All_Timeouts(timeoutPersister);
            Assert.IsTrue(timeouts.Count == 2);

            foreach (var timeout in timeouts)
            {
                TimeoutData timeoutData;
                timeoutPersister.TryRemove(timeout.Item1, out timeoutData);
            }

            Test_Helper.Assert_All_Timeouts_Have_Been_Removed(timeoutPersister);

            Test_Helper.Cleanup_Storage_Account(timeout1, timeoutPersister, false);
        }

        [Test]
        public void Should_remove_timeouts_by_id_and_return_true_using_new_interface()
        {
        }

        [Test]
        public void Should_remove_timeouts_by_sagaid()
        {
        }

        [Test]
        public void TryRemove_should_work_with_concurrent_operations()
        {
        }

        static class Test_Helper
        {
            internal static TimeoutPersister Create_TimeoutPersister()
            {
                return new TimeoutPersister
                {
                    ConnectionString = AzurePersistenceTests.GetConnectionString(),
                    PartitionKeyScope = new AzureTimeoutPersisterConfig().PartitionKeyScope
                };
            }


            internal static TimeoutData Generate_Timeout_With_Headers()
            {
                return new TimeoutData
                {
                    Time = DateTime.UtcNow.AddYears(-1),
                    Destination = new Address("timeouts", "some_azure_connection_string"),
                    SagaId = Guid.NewGuid(),
                    State = new byte[] { 1, 2, 3, 4 },
                    Headers = new Dictionary<string, string>
                    {
                        {"Prop1", "1234"},
                        {"Prop2", "text"}
                    },
                    OwningTimeoutManager = Configure.EndpointName
                };
            }

            internal static List<Tuple<string, DateTime>> Get_All_Timeouts(TimeoutPersister persister)
            {
                DateTime nextRun;
                var timeouts = persister.GetNextChunk(DateTime.Now.AddYears(-3), out nextRun).ToList();
                return timeouts;
            }

            public static void Assert_All_Timeouts_Have_Been_Removed(TimeoutPersister timeoutPersister)
            {
                var cloudStorageAccount = CloudStorageAccount.Parse(timeoutPersister.ConnectionString);

                var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(ServiceContext.TimeoutDataTableName);
                var results = table.ExecuteQuery(new TableQuery()).ToList();
                Assert.IsFalse(results.Any());
            }

            internal static void Cleanup_Storage_Account(TimeoutData timeoutDataToCleanup, TimeoutPersister timeoutPersister, bool deleteTimeoutDataRecords = true, bool deleteTimeDataManagerRecors = true)
            {
                var stateAddress = timeoutDataToCleanup.Id;
                var cloudStorageAccount = CloudStorageAccount.Parse(timeoutPersister.ConnectionString);

                if (deleteTimeoutDataRecords)
                {
                    // 1/3 - (state_address, empty)

                    var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(ServiceContext.TimeoutDataTableName);
                    var tableEntity = new DynamicTableEntity(stateAddress, string.Empty) { ETag = "*" };

                    table.Execute(TableOperation.Delete(tableEntity));
                    // 2/3 - (Date, state_address)
                    tableEntity = new DynamicTableEntity(timeoutDataToCleanup.Time.ToString(timeoutPersister.PartitionKeyScope), stateAddress)
                    {
                        ETag = "*"
                    };
                    table.Execute(TableOperation.Delete(tableEntity));
                    // 3/3 - (saga_id, state_address)
                    tableEntity = new DynamicTableEntity(timeoutDataToCleanup.SagaId.ToString(), stateAddress)
                    {
                        ETag = "*"
                    };
                    table.Execute(TableOperation.Delete(tableEntity));
                }

                if (deleteTimeDataManagerRecors)
                {
                    // 1/1 - (unique_endpoint_name, empty)
                    var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(ServiceContext.TimeoutManagerDataTableName);
                    var tableEntity = new DynamicTableEntity(Configure.EndpointName + "_" + Environment.MachineName, string.Empty)
                    {
                        ETag = "*"
                    };
                    table.Execute(TableOperation.Delete(tableEntity));
                }
            }

            public static void Remove_All_Blobs()
            {
                var cloudStorageAccount = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
                var blob = cloudStorageAccount.CreateCloudBlobClient().GetContainerReference("timeoutstate");
                blob.Delete(null, new BlobRequestOptions {RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(15), 5)});
            }
        }
    }
}