namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    public class AzureServiceBusTopologyCreator: NServiceBus.Transports.ICreateQueues
    {
        readonly ITopology topology;

        public AzureServiceBusTopologyCreator(ITopology topology)
        {
            this.topology = topology;
        }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            topology.Create(address);
        }
    }
}