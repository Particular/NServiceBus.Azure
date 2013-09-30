using NServiceBus;
using NServiceBus.Features;

namespace Hybrid.Client
{
    public class DisableAuditing : IWantCustomInitialization
    {
        public void Init()
        {
            Feature.Disable<Audit>();
        }
    }

    public class DisableTransactions : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Transactions.Disable();
        }
    }
}