using NServiceBus;

namespace Hybrid.Client
{
    public class OverrideTransport : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.AzureServiceBusMessageQueue();
        }
    }
}