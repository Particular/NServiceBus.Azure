namespace NServiceBus
{
    using System;
    using Logging;
    using Logging.Loggers;

    [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Message = "Trace logging is now built into the core of NSB.")]
    public static class SetLoggingLibraryForAzure
    {
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Message = "Trace logging is now built into the core of NSB.")]
        public static Configure ConsoleLogger(this Configure config)
        {
            throw new InvalidOperationException();
        }

    [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Message = "Trace logging is now built into the core of NSB.")]
        public static Configure TraceLogger(this Configure config)
        {
            throw new InvalidOperationException();
        }

// ReSharper disable UnusedParameter.Global
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Message = "Trace logging is now built into the core of NSB.")]
        public static void TraceLogger(this BusConfiguration config)
// ReSharper restore UnusedParameter.Global
        {
            LogManager.UseFactory(new TraceLoggerFactory());
        }
    }

}
