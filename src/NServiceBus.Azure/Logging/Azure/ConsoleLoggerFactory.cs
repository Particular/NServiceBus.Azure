namespace NServiceBus.Logging.Loggers
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    internal class ConsoleLoggerFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return new ConsoleLogger();
        }

        public ILog GetLogger(string name)
        {
            return new ConsoleLogger();
        }
    }
}