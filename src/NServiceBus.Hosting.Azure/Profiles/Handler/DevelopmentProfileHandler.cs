using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{

    internal class DevelopmentProfileHandler : IHandleProfile<Development>
    {
        void IHandleProfile.ProfileActivated(Configure config)
        {
            Configure.Instance.ConsoleLogger();
        }
    }
}