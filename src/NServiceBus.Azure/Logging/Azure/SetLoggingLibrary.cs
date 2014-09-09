namespace NServiceBus
{
    using System;
    using Logging;
    using Logging.Loggers;

    public static class SetLoggingLibraryForAzure
    {
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.TraceLogger()")]
        public static Configure ConsoleLogger(this Configure config)
        {
            throw new InvalidOperationException();
        }

        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.TraceLogger()")]
        public static Configure TraceLogger(this Configure config)
        {
            throw new InvalidOperationException();
        }

// ReSharper disable UnusedParameter.Global
        public static void TraceLogger(this BusConfiguration config)
// ReSharper restore UnusedParameter.Global
        {
            LogManager.UseFactory(new TraceLoggerFactory());
        }
    }

}
