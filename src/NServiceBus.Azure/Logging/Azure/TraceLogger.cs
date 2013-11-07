namespace NServiceBus.Logging.Loggers
{
    using System;
    using System.Diagnostics;

    /// <summary/>
    public class TraceLogger : ILog
    {
        /// <summary/>
        public bool IsDebugEnabled
        {
            get
            {
                return true;
            }
        }

        /// <summary/>
        public bool IsInfoEnabled
        {
            get
            {
                return true;
            }
        }

        public bool IsWarnEnabled
        {
            get
            {
                return true;
            }
        }

        public bool IsErrorEnabled
        {
            get
            {
                return true;
            }
        }

        public bool IsFatalEnabled
        {
            get
            {
                return true;
            }
        }

        public void Debug(string message)
        {
            Trace.TraceInformation(message);
        }

        public void Debug(string message, Exception exception)
        {
            Trace.TraceInformation(message);
            Trace.TraceInformation(exception.ToString());
        }

        public void DebugFormat(string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        public void Info(string message)
        {
            Trace.TraceInformation(message);
        }

        public void Info(string message, Exception exception)
        {
            Trace.TraceInformation(message);
            Trace.TraceInformation(exception.ToString());
        }

        public void InfoFormat(string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        public void Warn(string message)
        {
            Trace.TraceWarning(message);
        }

        public void Warn(string message, Exception exception)
        {
            Trace.TraceWarning(message);
            Trace.TraceWarning(exception.ToString());
        }

        public void WarnFormat(string format, params object[] args)
        {
            Trace.TraceWarning(format, args);
        }

        public void Error(string message)
        {
            Trace.TraceError(message);
        }

        public void Error(string message, Exception exception)
        {
            Trace.TraceError(message);
            Trace.TraceError(exception.ToString());
        }

        public void ErrorFormat(string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        public void Fatal(string message)
        {
            Trace.TraceError(message);
        }

        public void Fatal(string message, Exception exception)
        {
            Trace.TraceError(message);
            Trace.TraceError(exception.ToString());
        }

        public void FatalFormat(string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }
    }
}