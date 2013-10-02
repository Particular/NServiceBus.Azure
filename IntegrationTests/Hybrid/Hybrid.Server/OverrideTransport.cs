using NServiceBus;

namespace Hybrid.Server
{
    public class OverrideTransport : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.AzureServiceBusMessageQueue();
        }
    }
}