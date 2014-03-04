namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System.Collections.Generic;
    using System.IO;

    public interface ISendGatewayMessages
    {
        void Send(Stream message, IDictionary<string, string> headers, string destination);
    }
}