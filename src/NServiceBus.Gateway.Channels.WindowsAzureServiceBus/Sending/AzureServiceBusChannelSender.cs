namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System.Collections.Generic;
    using System.IO;

    [ChannelType("AzureServiceBus")]
    public class AzureServiceBusChannelSender : IChannelSender
    {
        public AzureServiceBusChannelSender()
        {

        }
    
        public void Send(string remoteAddress, IDictionary<string, string> headers, Stream data)
        {
            //todo IoC inject this
            new AzureServiceBusMessageQueueSender(new CreatesMessagingFactories())
                .Send(data, headers, remoteAddress);
        }
    }
}