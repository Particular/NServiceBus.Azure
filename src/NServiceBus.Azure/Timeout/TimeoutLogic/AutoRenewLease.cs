using System;
using System.Threading;

namespace NServiceBus.Azure
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    public class AutoRenewLease : IDisposable
    {
        public bool HasLease
        {
            get { return leaseId != null; }
        }

        readonly CloudBlockBlob blob;
        readonly string leaseId;
        Thread renewalThread;
        bool disposed;

        public AutoRenewLease(CloudBlockBlob blob)
        {
            this.blob = blob;
            blob.Container.CreateIfNotExists();
            leaseId = blob.TryAcquireLease();
            if (HasLease)
            {
                renewalThread = new Thread(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(40));
                    blob.RenewLease(new AccessCondition
                    {
                        LeaseId = leaseId
                    });
                });
                renewalThread.Start();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            if (renewalThread != null)
            {
                renewalThread.Abort();
                blob.ReleaseLease(new AccessCondition
                {
                    LeaseId = leaseId
                });
                renewalThread = null;
            }
            disposed = true;
        }
    }
}