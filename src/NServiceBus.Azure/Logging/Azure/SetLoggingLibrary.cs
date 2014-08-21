namespace NServiceBus
{
    using System;
    using Logging;
    using Logging.Loggers;

    public static class SetLoggingLibraryForAzure
    {
        public static Configure ConsoleLogger(this Configure config)
        {
            throw new InvalidOperationException();
        }

        public static Configure TraceLogger(this Configure config)
        {
            throw new InvalidOperationException();
        }

// ReSharper disable UnusedParameter.Global
        public static void TraceLogger(this ConfigurationBuilder config)
// ReSharper restore UnusedParameter.Global
        {
            LogManager.UseFactory(new TraceLoggerFactory());
        }
    }

}
