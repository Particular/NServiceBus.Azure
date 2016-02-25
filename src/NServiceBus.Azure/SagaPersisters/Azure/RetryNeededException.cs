namespace NServiceBus.SagaPersisters.Azure
{
    using System;

    public class RetryNeededException : Exception
    {
        public RetryNeededException() : base("This operation requires a retry as it wasn't possible to successfully process it now.")
        {
        }
    }
}