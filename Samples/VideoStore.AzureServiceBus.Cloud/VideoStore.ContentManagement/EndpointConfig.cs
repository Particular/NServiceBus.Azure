using NServiceBus.Config;
using NServiceBus.Features;
using NServiceBus.Unicast.Queuing.Azure.ServiceBus;

namespace VideoStore.ContentManagement
{
    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Worker, UsingTransport<AzureServiceBus> { }
    
    public class MyClass : IWantToRunWhenBusStartsAndStops
    {
        public void Start()
        {
            Console.Out.WriteLine("The VideoStore.ContentManagement endpoint is now started and subscribed to OrderAccepted events from VideoStore.Sales");
        }

        public void Stop()
        {

        }
    }

    /// <summary>
    /// This is just here so that topics are created irrespective of boot order of the processes
    /// </summary>
    public class AutoCreateDependantTopics : IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            var topicCreator = new AzureServicebusTopicCreator();

            topicCreator.Create(Address.Parse("VideoStore.Sales"));
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
