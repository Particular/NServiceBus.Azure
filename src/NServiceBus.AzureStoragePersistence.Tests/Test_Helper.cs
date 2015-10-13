namespace NServiceBus.AzureStoragePersistence.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using NServiceBus.Azure;
    using NServiceBus.Config;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    static class Test_Helper
    {
        internal static TimeoutPersister Create_TimeoutPersister()
        {
            TimeoutPersister persister = null;
            try
            {
                persister = new TimeoutPersister
                {
                    PartitionKeyScope = new AzureTimeoutPersisterConfig().PartitionKeyScope,
                    ConnectionString = AzurePersistenceTests.GetConnectionString()
                };
            }
            catch (WebException exception)
            {
                // Azure blob container CreateIfNotExists() can falsly report HTTP 409 error, swallow it
                if (exception.Status != WebExceptionStatus.ProtocolError || (exception.Response is HttpWebResponse && ((HttpWebResponse)exception.Response).StatusCode != HttpStatusCode.NotFound))
                {
                    throw;
                }
            }
            return persister;
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

        internal static TimeoutData Getnerate_Timeout_With_Saga_Id(Guid sagaId)
        {
            var timeoutWithHeaders1 = Generate_Timeout_With_Headers();
            timeoutWithHeaders1.SagaId = sagaId;
            return timeoutWithHeaders1;
        }

        internal static List<Tuple<string, DateTime>> Get_All_Timeouts_Using_GetNextChunk(TimeoutPersister persister)
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

        internal static void Perform_Storage_Cleanup()
        {
            Remove_All_Rows_For_Table(ServiceContext.TimeoutDataTableName);
            Remove_All_Rows_For_Table(ServiceContext.TimeoutManagerDataTableName);
            Remove_All_Blobs();
        }

        private static void Remove_All_Rows_For_Table(string tableName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(tableName);

            var projectionQuery = new TableQuery<DynamicTableEntity>().Select(new[] { "Destination" });

            // Define an entity resolver to work with the entity after retrieval.
            EntityResolver<Tuple<string, string>> resolver = (pk, rk, ts, props, etag) => props.ContainsKey("Destination") ? new Tuple<string, string>(pk, rk) : null;

            foreach (var tuple in table.ExecuteQuery(projectionQuery, resolver, null, null))
            {
                var tableEntity = new DynamicTableEntity(tuple.Item1, tuple.Item2) { ETag = "*" };
                table.Execute(TableOperation.Delete(tableEntity));
            }
        }

        private static void Remove_All_Blobs()
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(AzurePersistenceTests.GetConnectionString());
            var container = cloudStorageAccount.CreateCloudBlobClient().GetContainerReference("timeoutstate");
            foreach (var blob in container.ListBlobs())
            {
                ((ICloudBlob)blob).Delete(DeleteSnapshotsOption.None, AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions { RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(15), 5) });

            }
        }
    }
}