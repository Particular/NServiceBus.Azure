namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Linq;
    using Microsoft.ServiceBus.Messaging;
    using Settings;
    using Unicast;

    public static class BrokeredMessageConverter
    {
        public static TransportMessage ToTransportMessage(this BrokeredMessage message)
        {
            TransportMessage t;
            var rawMessage = message.GetBody<byte[]>() ?? new byte[0];

            if (message.Properties.Count > 0)
            {
                t = new TransportMessage(message.MessageId,
                    message.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as string),
                    !String.IsNullOrWhiteSpace( message.ReplyTo ) ? Address.Parse( message.ReplyTo ) : null)
                {
                    CorrelationId = message.CorrelationId,
                    TimeToBeReceived = message.TimeToLive,
                    MessageIntent = (MessageIntentEnum)
                        Enum.Parse(typeof(MessageIntentEnum), message.Properties[Headers.MessageIntent].ToString())
                };

                t.Body = rawMessage;
            }
            else
            {
                t = new TransportMessage
                {
                    Body = rawMessage
                };
            }

            return t;
        }

        public static BrokeredMessage ToBrokeredMessage(this TransportMessage message, PublishOptions options, ReadOnlySettings settings)
        {
            var brokeredMessage = message.Body != null ? new BrokeredMessage(message.Body) : new BrokeredMessage();

            brokeredMessage.CorrelationId = message.CorrelationId;
            if (message.TimeToBeReceived < TimeSpan.MaxValue) brokeredMessage.TimeToLive = message.TimeToBeReceived;

            foreach (var header in message.Headers)
            {
                brokeredMessage.Properties[header.Key] = header.Value;
            }

            brokeredMessage.Properties[Headers.MessageIntent] = message.MessageIntent.ToString();
            brokeredMessage.MessageId = message.Id;

            if (message.ReplyToAddress != null)
            {
                brokeredMessage.ReplyTo = new DeterminesBestConnectionStringForAzureServiceBus().Determine(settings, message.ReplyToAddress);
            }
            else if (options.ReplyToAddress != null)
            {
                brokeredMessage.ReplyTo = new DeterminesBestConnectionStringForAzureServiceBus().Determine(settings, options.ReplyToAddress);
            }

            if (message.TimeToBeReceived < TimeSpan.MaxValue)
            {
                brokeredMessage.TimeToLive = message.TimeToBeReceived;
            }

            return brokeredMessage;
        }

        public static BrokeredMessage ToBrokeredMessage(this TransportMessage message, SendOptions options, SettingsHolder settings, bool expectDelay)
        {
            var brokeredMessage = message.Body != null ? new BrokeredMessage(message.Body) : new BrokeredMessage();

            var timeToSend = DelayIfNeeded(options, expectDelay);

            brokeredMessage.CorrelationId = message.CorrelationId;
                        
            if (timeToSend.HasValue)
                brokeredMessage.ScheduledEnqueueTimeUtc = timeToSend.Value;

            foreach (var header in message.Headers)
            {
                brokeredMessage.Properties[header.Key] = header.Value;
            }

            brokeredMessage.Properties[Headers.MessageIntent] = message.MessageIntent.ToString();
            brokeredMessage.MessageId = message.Id;

            if (options.ReplyToAddress != null)
            {
                brokeredMessage.ReplyTo = new DeterminesBestConnectionStringForAzureServiceBus().Determine(settings, options.ReplyToAddress);
            }

            if (options.TimeToBeReceived.HasValue && options.TimeToBeReceived < TimeSpan.MaxValue)
            {
                brokeredMessage.TimeToLive = options.TimeToBeReceived.Value;
            }

            if (brokeredMessage.Size > 256*1024)
            {
                throw new MessageTooLargeException(string.Format("The message with id {0} is larger that the maximum message size allowed by Azure ServiceBus, consider using the databus instead", message.Id));
            }

            return brokeredMessage;
        }
        
        private static DateTime? DelayIfNeeded(SendOptions options, bool expectDelay)
        {
            DateTime? deliverAt = null;

            if (options.DelayDeliveryWith.HasValue)
            {
                deliverAt = DateTime.UtcNow + options.DelayDeliveryWith.Value;
            }
            else
            {
                if (options.DeliverAt.HasValue)
                {
                    deliverAt = options.DeliverAt.Value;
                }
                else if (expectDelay)
                {
                    throw new ArgumentException("A delivery time needs to be specified for Deferred messages");
                }

            }

            return deliverAt;
        }
    }
}