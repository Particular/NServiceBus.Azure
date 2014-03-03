namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System;
    using System.Transactions;

    internal class SendResourceManager : IEnlistmentNotification
    {
        private readonly Action onCommit;
       
        public SendResourceManager(Action onCommit)
        {
            this.onCommit = onCommit;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            onCommit();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}