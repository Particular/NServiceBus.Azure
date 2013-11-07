using NServiceBus.Hosting.Profiles;
using NServiceBus.Logging;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    using Logging.Loggers;

    internal class DevelopmentProfileHandler : IHandleProfile<Development>
    {
        void IHandleProfile.ProfileActivated()
        {
            if (LogManager.LoggerFactory is NullLoggerFactory)
                Configure.Instance.ConsoleLogger();
        }
    }
}