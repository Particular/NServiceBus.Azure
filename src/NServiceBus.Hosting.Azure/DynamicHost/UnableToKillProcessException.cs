namespace NServiceBus.Hosting
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class UnableToKillProcessException : Exception
    {
        public UnableToKillProcessException() { }

        public UnableToKillProcessException(string message) : base(message) { }

        public UnableToKillProcessException(string message, Exception inner) : base(message, inner) { }

        protected UnableToKillProcessException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}