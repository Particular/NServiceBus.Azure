namespace NServiceBus.Logging.Loggers
{
    using System;

    public class TraceLoggerFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return (ILog)new TraceLogger();
        }

        public ILog GetLogger(string name)
        {
            return (ILog)new TraceLogger();
        }
    }
}