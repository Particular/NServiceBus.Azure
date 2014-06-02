namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Linq;
    using Microsoft.ServiceBus.Messaging;

    public static class BrokeredMessageConverter
    {
        public static TransportMessage ToTransportMessage(BrokeredMessage message)
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
    }
}