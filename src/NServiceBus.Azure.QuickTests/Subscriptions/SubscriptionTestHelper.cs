using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NServiceBus;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

public class SubscriptionTestHelper
{
    internal static ISubscriptionStorage CreateAzureSubscriptionStorage()
    {
        var account = CloudStorageAccount.Parse(GetConnectionString());

        var table = account.CreateCloudTableClient().GetTableReference(SubscriptionServiceContext.SubscriptionTableName);
        table.CreateIfNotExists();

        return new AzureSubscriptionStorage(account);
    }

    internal static void PerformStorageCleanup()
    {
        RemoveAllRowsForTable(AzureSubscriptionStorageDefaults.TableName);
    }

    static void RemoveAllRowsForTable(string tableName)
    {
        var cloudStorageAccount = CloudStorageAccount.Parse(GetConnectionString());
        var table = cloudStorageAccount.CreateCloudTableClient().GetTableReference(tableName);

        table.CreateIfNotExists();

        var projectionQuery = new TableQuery<DynamicTableEntity>().Select(new[]
        {
            "Destination"
        });

        EntityResolver<Tuple<string, string>> resolver = (pk, rk, ts, props, etag) => props.ContainsKey("Destination") ? new Tuple<string, string>(pk, rk) : null;

        foreach (var tuple in table.ExecuteQuery(projectionQuery, resolver))
        {
            var tableEntity = new DynamicTableEntity(tuple.Item1, tuple.Item2)
            {
                ETag = "*"
            };

            try
            {
                table.Execute(TableOperation.Delete(tableEntity));
            }
            catch (StorageException)
            {
            }
        }
    }

    static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("AzureStoragePersistence.ConnectionString");
    }
}