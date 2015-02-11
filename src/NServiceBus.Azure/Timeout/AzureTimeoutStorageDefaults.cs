namespace NServiceBus
{
    public class AzureTimeoutStorageDefaults
    {
        public const string ConnectionString = "UseDevelopmentStorage=true";
        public const string TimeoutManagerDataTableName = "TimeoutManagerDataTable";
        public const string TimeoutDataTableName = "TimeoutDataTableName";
        /// <summary>
        /// Catchup interval in seconds. Default is 1 hour.
        /// </summary>
        public const int CatchUpInterval = 3600;
        public const string PartitionKeyScope = "yyyyMMddHH";
        public const bool CreateSchema = true;
    }
}