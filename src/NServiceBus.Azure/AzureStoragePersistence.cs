namespace NServiceBus
{
    using Features;
    using Persistence;

    public class AzureStoragePersistence : PersistenceDefinition
    {
        internal AzureStoragePersistence()
        {
            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<AzureStorageTimeoutPersistence>());
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<AzureStorageSagaPersistence>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<AzureStorageSubscriptionPersistence>());
        }
    }
}