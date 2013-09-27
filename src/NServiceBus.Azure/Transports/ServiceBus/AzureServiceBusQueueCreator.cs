namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Microsoft.ServiceBus.Messaging;
    using Transports;

    /// <summary>
    /// Creates the queues. Note that this class will only be invoked when running the windows host and not when running in the fabric
    /// </summary>
    public class AzureServiceBusQueueCreator:ICreateQueues
    {
        readonly ICreateQueueClients queueCreator;

        public AzureServiceBusQueueCreator(ICreateQueueClients queueCreator)
        {
            queueCreator = queueCreator;
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