namespace NServiceBus.Timeout
{
    using System;
    using System.Text.RegularExpressions;

    class AzureTimeoutStorageGuard
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

        public static void CheckCatchUpInterval(object catchUpInterval)
        {
            if ((int)catchUpInterval < 1)
            {
                throw new ArgumentOutOfRangeException("catchUpInterval", catchUpInterval, "catchUpInterval should not be less than 1 second.");
            }
        }

        public static void CheckPartitionKeyScope(object partitionKeyScope)
        {
            if (string.IsNullOrWhiteSpace((string)partitionKeyScope))
            {
                throw new ArgumentException("Partition key scope should not be an empty string.", "partitionKeyScope");
            }

            var partitionKeyRegex = new Regex(@"^[a-zA-Z0-9\-_]{4,}$");
            if (!partitionKeyRegex.IsMatch((string)partitionKeyScope))
            {
                const string errorMessage = "Invalid partition key scope. Valid key should follow the following regular expression:"
                                            + @" ^[a-zA-Z0-9\-_]{4,}$ "
                                            + " and comply with .NET DateTime formatting string rules. Examples are:\n"
                                            + @" yyyy-MM-dd-mm\n"
                                            + @" yyyyMMddHH (default)\n"
                                            + @" yyyy_MM_dd";
                throw new ArgumentException(errorMessage, "partitionKeyScope");
            }
        }
    }
}
