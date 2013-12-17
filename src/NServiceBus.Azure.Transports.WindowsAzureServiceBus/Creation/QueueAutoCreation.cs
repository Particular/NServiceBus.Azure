namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Linq;
    using Config;
    using Features;
    using NServiceBus.Transports;
    using Unicast.Queuing;

    /// <summary>
    /// Makes sure that all queues are created
    /// </summary>
    public class QueueAutoCreation : Feature, IWantToRunWhenConfigurationIsComplete
    {
        public ICreateQueues QueueCreator { get; set; }

        public void Run()
        {
            if (!ShouldAutoCreate)
                return;

            var wantQueueCreatedInstances = Configure.Instance.Builder.BuildAll<IWantQueueCreated>().ToList();

            foreach (var wantQueueCreatedInstance in wantQueueCreatedInstances.Where(wantQueueCreatedInstance => !wantQueueCreatedInstance.IsDisabled))
            {
                if (wantQueueCreatedInstance.Address == null)
                {
                    throw new InvalidOperationException(string.Format("IWantQueueCreated implementation {0} returned a null address", wantQueueCreatedInstance.GetType().FullName));
                }

                QueueCreator.CreateQueueIfNecessary(wantQueueCreatedInstance.Address, null);
            }
        }

        internal static bool ShouldAutoCreate
        {
            get
            {
                var shouldNotAutoCreate = !IsEnabled<QueueAutoCreation>() || ConfigureQueueCreation.DontCreateQueues;

                if (shouldNotAutoCreate)
                {
                    // force both to be false, as this is currently not guaranteed
                    Disable<QueueAutoCreation>();
                    Configure.Instance.DoNotCreateQueues();
                }

                return !shouldNotAutoCreate;
            }
        }
    }
}