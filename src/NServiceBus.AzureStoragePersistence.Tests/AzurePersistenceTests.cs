namespace NServiceBus.AzureStoragePersistence.Tests
{
    using System;

    public class AzurePersistenceTests
    {
        public static string GetConnectionString()
        {
            var environmentVariable = Environment.GetEnvironmentVariable("AzureStorageQueue.ConnectionString");
            if (environmentVariable != null)
            {
                return environmentVariable;
            }

            throw new Exception("Can't run Azure persistence tests - environment variable `AzureStorageQueue.ConnectionString` with Azure storage connection string was not found.");
        }
    }
}
