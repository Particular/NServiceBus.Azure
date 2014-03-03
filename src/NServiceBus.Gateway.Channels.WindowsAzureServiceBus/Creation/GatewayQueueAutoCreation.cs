namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
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
    public class GatewayQueueAutoCreation : Feature, IWantToRunWhenConfigurationIsComplete
    {
        public ICreateQueues QueueCreator { get; set; }

        public void Run()
        {
            if (!ShouldAutoCreate)
                return;

            //todo loop through gateway config to find the queues to be created

            //var wantQueueCreatedInstances = Configure.Instance.Builder.BuildAll<IWantQueueCreated>().ToList();

            //foreach (var wantQueueCreatedInstance in wantQueueCreatedInstances.Where(wantQueueCreatedInstance => !wantQueueCreatedInstance.IsDisabled))
            //{
            //    if (wantQueueCreatedInstance.Address == null)
            //    {
            //        throw new InvalidOperationException(string.Format("IWantQueueCreated implementation {0} returned a null address", wantQueueCreatedInstance.GetType().FullName));
            //    }

            //    var username = Thread.CurrentPrincipal != null ? (Thread.CurrentPrincipal.Identity != null ? Thread.CurrentPrincipal.Identity.Name : null) : null;
            //    QueueCreator.CreateQueueIfNecessary(wantQueueCreatedInstance.Address, username);
            //}
        }

        internal static bool ShouldAutoCreate
        {
            get
            {
                return IsEnabled<GatewayQueueAutoCreation>();
            }
        }

        public override bool IsEnabledByDefault
        {
            get { return true; }
        }

    }

}