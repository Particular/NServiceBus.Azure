using NServiceBus;

namespace Hybrid.Client
{
    public class OverrideTransport : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.AzureServiceBusMessageQueue();
        }
    }
}