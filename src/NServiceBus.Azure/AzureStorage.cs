namespace NServiceBus
{
    using Persistence;

    public class AzureStorage : PersistenceDefinition
    {
        internal AzureStorage()
        {
            Supports(Storage.Timeouts);
            Supports(Storage.Sagas);
            Supports(Storage.Subscriptions);
        }
    }
}