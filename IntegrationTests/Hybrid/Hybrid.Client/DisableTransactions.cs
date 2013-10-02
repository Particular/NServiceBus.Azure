using NServiceBus;

namespace Hybrid.Client
{
    public class DisableTransactions : INeedInitialization
    {
        public void Init()
        {
            Configure.Transactions.Disable();
        }
    }
}