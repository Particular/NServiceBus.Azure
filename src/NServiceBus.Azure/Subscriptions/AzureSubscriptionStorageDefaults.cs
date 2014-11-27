namespace NServiceBus
{
    public class AzureSubscriptionStorageDefaults
    {
        public const string ConnectionString = "UseDevelopmentStorage=true";
        public const bool CreateSchema = true;
        public const string TableName = "Subscription";
    }
}
