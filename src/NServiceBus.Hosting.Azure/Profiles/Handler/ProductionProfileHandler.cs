using NServiceBus.Hosting.Profiles;
using NServiceBus.Logging;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    using Logging.Loggers;

    internal class ProductionProfileHandler : IHandleProfile<Production>
    {
        void IHandleProfile.ProfileActivated()
        {
            if (LogManager.LoggerFactory is NullLoggerFactory)
                Configure.Instance.TraceLogger();
        }
    }
}