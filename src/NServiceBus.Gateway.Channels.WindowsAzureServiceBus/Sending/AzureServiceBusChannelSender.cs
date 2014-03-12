namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using IChannelSender = V2.Channels.IChannelSender;

    [V2.Channels.ChannelType("AzureServiceBus")]
    internal class AzureServiceBusChannelSender : IChannelSender
    {
        readonly ISendGatewayMessages gatewayQueueSender;

        public AzureServiceBusChannelSender(ISendGatewayMessages gatewayQueueSender)
        {
            this.gatewayQueueSender = gatewayQueueSender;
        }

        public void Send(string remoteAddress, IDictionary<string, string> headers, Stream data)
        {
            gatewayQueueSender.Send(data, headers, remoteAddress);
        }
    }
}