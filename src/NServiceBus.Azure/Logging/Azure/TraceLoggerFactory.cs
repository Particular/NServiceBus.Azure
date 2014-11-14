namespace NServiceBus.Logging.Loggers
{
    using System;

    [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Message = "Trace logging is now built into the core of NSB.")]
    public class TraceLoggerFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return new TraceLogger();
        }

        public ILog GetLogger(string name)
        {
            return new TraceLogger();
        }
    }
}