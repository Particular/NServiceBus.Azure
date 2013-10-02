using NServiceBus;

namespace Hybrid.Server
{
    public class DisableTransactions : INeedInitialization
    {
        public void Init()
        {
            Configure.Transactions.Disable();
        }
    }
}