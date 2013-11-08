namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Transports;

    /// <summary>
    /// Creates the queues. Note that this class will only be invoked when running the windows host and not when running in the fabric
    /// </summary>
    public class AzureServiceBusQueueCreator:ICreateQueues
    {
        readonly ICreateQueueClients queueCreator;

        public AzureServiceBusQueueCreator(ICreateQueueClients queueCreator)
        {
            this.queueCreator = queueCreator;
        }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            try
            {
                queueCreator.CreateQueue(address);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // is ok.
            }
        }
    }
}