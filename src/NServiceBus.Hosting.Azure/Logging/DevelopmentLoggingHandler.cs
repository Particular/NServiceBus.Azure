namespace NServiceBus.Hosting.Azure.LoggingHandlers
{
    /// <summary>
    /// Handles logging configuration for the development profile
    /// </summary>
    public class DevelopmentLoggingHandler : Profiles.IConfigureLoggingForProfile<Development>
    {
        void Profiles.IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            // startup logging is handled outside nsb by wadcfg file
        }
    }
}