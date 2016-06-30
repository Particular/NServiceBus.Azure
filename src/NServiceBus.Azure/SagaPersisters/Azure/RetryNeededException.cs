namespace NServiceBus.SagaPersisters.Azure
{
    using System;

    public class RetryNeededException : Exception
    {
        const string errorMessage = "This operation requires a retry as it wasn't possible to successfully process it now.";
        public RetryNeededException() : base(errorMessage)
        {
        }

        public RetryNeededException(Exception innerException) : base(errorMessage, innerException)
        {
        }
    }
}