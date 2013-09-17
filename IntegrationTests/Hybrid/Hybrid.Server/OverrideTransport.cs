using NServiceBus;

namespace Hybrid.Server
{
    public class OverrideTransport : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.AzureServiceBusMessageQueue();
        }
    }
}