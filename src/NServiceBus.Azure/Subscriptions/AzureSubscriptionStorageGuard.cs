namespace NServiceBus.Subscriptions
{
    using System;
    using System.Text.RegularExpressions;

    class AzureSubscriptionStorageGuard
    {
        public static void CheckConnectionString(object connectionString)
        {
            if (string.IsNullOrWhiteSpace((string)connectionString))
            {
                throw new ArgumentException("ConnectionString should not be an empty string.", "connectionString");
            }
        }

        public static void CheckTableName(object tableName)
        {
            if (string.IsNullOrWhiteSpace((string)tableName))
            {
                throw new ArgumentException("Table name should not be an empty string.", "tableName");
            }

            var tableNameRegex = new Regex("^[A-Za-z][A-Za-z0-9]{2,62}(?!tables)$");
            if (!tableNameRegex.IsMatch((string)tableName))
            {
                // error message is following MSFT guidelines http://msdn.microsoft.com/library/azure/dd179338.aspx
                const string errorMessage = "Invalid table name. Valid name should follow these rules:\n"
                                            + " Contain only alphanumeric characters.\n"
                                            + " Cannot begin with a numeric character.\n"
                                            + " Must be from 3 to 63 characters long."
                                            + " Avoid reserved names, such as \"tables\".";
                throw new ArgumentException(errorMessage, "tableName");
            }
        }
    }
}
