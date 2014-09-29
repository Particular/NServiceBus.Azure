namespace NServiceBus
{
    using Azure.Transports.WindowsAzureStorageQueues;
    using Configuration.AdvanceExtensibility;

    public static class AzureStorageTransportExtensions
    {
        /// <summary>
        /// Sets the amount of time, in milliseconds, to add to the time to wait before checking for a new message
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TransportExtensions<AzureStorageQueueTransport> PeekInterval(this TransportExtensions<AzureStorageQueueTransport> config, int value)
        {
            config.GetSettings().SetProperty<AzureMessageQueueReceiver>(t => t.PeekInterval, value);
            return config;
        }

        /// <summary>
        /// Sets the maximum amount of time, in milliseconds, that the queue will wait before checking for a new message
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TransportExtensions MaximumWaitTimeWhenIdle(this TransportExtensions<AzureStorageQueueTransport> config, int value)
        {
            config.GetSettings().SetProperty<AzureMessageQueueReceiver>(t => t.MaximumWaitTimeWhenIdle, value);

            return config;
        }

        /// <summary>
        /// Controls how long messages should be invisible to other callers when receiving messages from the queue
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TransportExtensions<AzureStorageQueueTransport> MessageInvisibleTime(this TransportExtensions<AzureStorageQueueTransport> config, int value)
        {
            config.GetSettings().SetProperty<AzureMessageQueueReceiver>(t => t.MessageInvisibleTime, value);

            return config;
        }

        /// <summary>
        /// Controls how many messages should be read from the queue at once
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TransportExtensions<AzureStorageQueueTransport> BatchSize(this TransportExtensions<AzureStorageQueueTransport> config, int value)
        {
            config.GetSettings().SetProperty<AzureMessageQueueReceiver>(t => t.BatchSize, value);

            return config;
        }
    }
}