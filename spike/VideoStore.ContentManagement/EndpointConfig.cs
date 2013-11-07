using System.Diagnostics;
using NServiceBus.Config;
using NServiceBus.Features;
using NServiceBus.Unicast.Queuing.Azure.ServiceBus;

namespace VideoStore.ContentManagement
{
    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Worker, UsingTransport<AzureStorageQueue> { }
    
    public class MyClass : IWantToRunWhenBusStartsAndStops
    {
        public void Start()
        {
            Trace.WriteLine(string.Format("The VideoStore.ContentManagement endpoint is now started and subscribed to OrderAccepted events from VideoStore.Sales"));
        }

        public void Stop()
        {

        }
    }

    // We don't need it, so instead of configuring it, we disable it
    public class DisableTimeoutManager : INeedInitialization
    {
        public void Init()
        {
            Feature.Disable<TimeoutManager>();
        }
    }
}
