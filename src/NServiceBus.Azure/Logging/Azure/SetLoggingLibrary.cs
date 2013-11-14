namespace NServiceBus
{
    using System;
    using Logging;
    using Logging.Loggers;

    public static class SetLoggingLibraryForAzure
    {
        public static Configure ConsoleLogger(this Configure config)
        {
            LogManager.LoggerFactory = new ConsoleLoggerFactory();
            return config;
        }

        public static Configure TraceLogger(this Configure config)
        {
            LogManager.LoggerFactory = new TraceLoggerFactory();
            return config;
        }

        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0", Replacement = "TraceLogger")] 
        public static Configure AzureDiagnosticsLogger(this Configure config, bool enable = true, bool initialize = true)
        {
            throw new NotSupportedException("Azure Diagnostics Logger is not supported anymore, setup logging using the .wadcfg file and use TraceLogger instead.");
        }
    }

}
