namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;
    using System.Linq;
    using System.Threading;
    using Config;
    using Features;
    using NServiceBus.Transports;
    using Unicast.Queuing;

    /// <summary>
    /// Makes sure that all queues are created
    /// </summary>
    public class QueueAutoCreation:Feature,IWantToRunWhenConfigurationIsComplete
    {
        public ICreateQueues QueueCreator { get; set; }

        public void Run(Configure config)
        {
            if (!IsEnabled<QueueAutoCreation>() || ConfigureQueueCreation.DontCreateQueues)
                return;

            var wantQueueCreatedInstances = Configure.Instance.Builder.BuildAll<IWantQueueCreated>().ToList();

            foreach (var wantQueueCreatedInstance in wantQueueCreatedInstances.Where(wantQueueCreatedInstance => wantQueueCreatedInstance.ShouldCreateQueue(config)))
            {
                if (wantQueueCreatedInstance.Address == null)
                {
                    throw new InvalidOperationException(string.Format("IWantQueueCreated implementation {0} returned a null address", wantQueueCreatedInstance.GetType().FullName));
                }

                var username = Thread.CurrentPrincipal != null ? (Thread.CurrentPrincipal.Identity != null ? Thread.CurrentPrincipal.Identity.Name : null) : null;
                QueueCreator.CreateQueueIfNecessary(AzureQueueAddressConvention.Apply(wantQueueCreatedInstance.Address),username);
            }
        }

        public override bool IsEnabledByDefault
        {
            get { return true; }
        }
    }
}