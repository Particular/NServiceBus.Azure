namespace NServiceBus
{
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

    }

}
