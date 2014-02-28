namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.WindowsAzure.Storage.Queue;

    public class EnvelopeDeserializationFailed:SerializationException
    {
        CloudQueueMessage message;


        public EnvelopeDeserializationFailed(CloudQueueMessage message, Exception ex)
            : base("Failed to deserialize message envelope", ex)
        {
            this.message = message;
        }

        public CloudQueueMessage FailedMessage
        {
            get { return message; }
        }
    }
}