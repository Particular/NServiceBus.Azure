namespace NServiceBus
{
    using Transports;

    /// <summary>
    /// Transport definition for AzureStorageQueue
    /// </summary>
    public class AzureStorageQueue : TransportDefinition
    {
        public AzureStorageQueue()
        {
            HasSupportForDistributedTransactions = false;
        }
    }
}