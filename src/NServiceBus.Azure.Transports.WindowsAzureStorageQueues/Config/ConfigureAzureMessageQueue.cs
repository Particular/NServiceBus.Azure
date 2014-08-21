namespace NServiceBus
{
    using System;

// ReSharper disable UnusedParameter.Global
    public static class ConfigureAzureMessageQueue
    {

        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UseTransport<AzureStorageQueue>()")]
        public static Configure AzureMessageQueue(this Configure config)
        {
            throw new InvalidOperationException();
        }

        
        [ObsoleteEx(RemoveInVersion = "7", TreatAsErrorFromVersion = "5.4", Replacement = "config.UseTransport<AzureStorageQueue>().PeekInterval()")]
        public static Configure PeekInterval(this Configure config, int value)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Sets the maximum amount of time, in milliseconds, that the queue will wait before checking for a new message
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure MaximumWaitTimeWhenIdle(this Configure config, int value)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Controls how long messages should be invisible to other callers when receiving messages from the queue
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure MessageInvisibleTime(this Configure config, int value)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Controls how many messages should be read from the queue at once
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure BatchSize(this Configure config, int value)
        {
            throw new InvalidOperationException();
        }
    }
// ReSharper restore UnusedParameter.Global
}