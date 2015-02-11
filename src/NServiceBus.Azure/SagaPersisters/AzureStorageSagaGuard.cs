namespace NServiceBus.SagaPersisters
{
    using System;

    class AzureStorageSagaGuard
    {
        public static void CheckConnectionString(object connectionString)
        {
            if (string.IsNullOrWhiteSpace((string)connectionString))
            {
                throw new ArgumentException("ConnectionString should not be an empty string.", "connectionString");
            }
        }      
    }
}