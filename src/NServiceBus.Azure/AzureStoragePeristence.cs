namespace NServiceBus
{
    using Features;
    using Persistence;

    public class AzureStoragePeristence : PersistenceDefinition
    {
        internal AzureStoragePeristence()
        {
            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<AzureStorageTimeoutPersistence>());
            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<AzureStorageSagaPersistence>());
            Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<AzureStorageSubscriptionPersistence>());
        }
    }
}