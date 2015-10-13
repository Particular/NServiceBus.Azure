namespace NServiceBus.Logging.Loggers
{
    using System;

    public class TraceLoggerFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            // ReSharper disable once RedundantCast
            return (ILog)new TraceLogger();
        }

        public ILog GetLogger(string name)
        {
            // ReSharper disable once RedundantCast
            return (ILog)new TraceLogger();
        }
    }
}