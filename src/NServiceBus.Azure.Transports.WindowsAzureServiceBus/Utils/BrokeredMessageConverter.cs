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
    }
}