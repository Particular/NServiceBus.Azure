using NServiceBus.Features;

namespace VideoStore.Operations
{
    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Worker, UsingTransport<AzureStorageQueue>
    {
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
