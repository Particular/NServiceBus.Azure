namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System.Configuration;
    using Features;
    using NServiceBus.Config;
    using Unicast.Transport;

    /// <summary>
    /// Makes sure that all queues are created
    /// </summary>
    public class GatewayQueueAutoCreation : Feature, IWantToRunWhenConfigurationIsComplete
    {
        public ICreateGatewayQueues QueueCreator { get; set; }
   
        public void Run()
        {
            if (!ShouldAutoCreate)
                return;

            var gatewayConfig = Configure.GetConfigSection<V2.Config.GatewayConfig>();

            foreach (var channel in gatewayConfig.GetChannels())
            {
                if (channel.Type.ToLower() == "azureservicebus")
                {
                    var address = channel.Address;
                    var @namespace = TransportConnectionString.GetConnectionStringOrNull("NServiceBus/Gateway/" + address);

                    if (@namespace == null)
                    {
                        throw new ConfigurationErrorsException(string.Format("No connection string has been defined for the channel {0}", address));
                    }
                    
                    QueueCreator.Create(address, @namespace);
                }
            }
        }

        internal static bool ShouldAutoCreate
        {
            get
            {
                return IsEnabled<GatewayQueueAutoCreation>();
            }
        }

        public override bool IsEnabledByDefault
        {
            get { return true; }
        }

    }

}