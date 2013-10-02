using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    using System;

    internal class OnAzureTableStorageProfileHandler : IHandleProfile<OnAzureTableStorage>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            throw new NotSupportedException("Registering the storage infrastructure using a profile is no longer supported, please override the storage infrastructure using INeedInitialization instead.");
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}