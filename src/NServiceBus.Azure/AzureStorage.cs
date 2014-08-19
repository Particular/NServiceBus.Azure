namespace NServiceBus
{
    using Features;
    using Persistence;

    public class AzureStorage : PersistenceDefinition
    {
        internal AzureStorage()
        {
            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<AzureStorageTimeoutPersistence>());
            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<AzureStorageSagaPersistence>());
            Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<AzureStorageSubscriptionPersistence>());
        }
    }
}