namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;
    using System.Transactions;
    using Microsoft.WindowsAzure.Storage.Queue;
    
    public class SendResourceManager : IEnlistmentNotification
    {
        private readonly CloudQueue queue;
        private readonly CloudQueueMessage message;
        readonly TimeSpan? timeToBeReceived;

        public SendResourceManager(CloudQueue queue, CloudQueueMessage message, TimeSpan? timeToBeReceived)
        {
            this.queue = queue;
            this.message = message;
            this.timeToBeReceived = timeToBeReceived;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            queue.AddMessage(message, timeToBeReceived);
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