namespace NServiceBus
{
    using Features;
    using Persistence;

    public class AzureStoragePersistence : PersistenceDefinition
    {
        internal AzureStoragePersistence()
        {
            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<AzureStorageTimeoutPersistence>());
            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<AzureStorageSagaPersistence>());
            Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<AzureStorageSubscriptionPersistence>());
        }
    }
}