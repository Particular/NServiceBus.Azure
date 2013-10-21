namespace VideoStore.Sales
{
    using System;
    using System.Diagnostics;
    using VideoStore.Common;
    using VideoStore.Messages.Commands;
    using VideoStore.Messages.Events;
    using NServiceBus;

    public class SubmitOrderHandler : IHandleMessages<SubmitOrder>
    {
        public IBus Bus { get; set; }

        public void Handle(SubmitOrder message)
        {
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }

            Trace.WriteLine(string.Format("We have received an order #{0} for [{1}] video(s).", message.OrderNumber,
                                  String.Join(", ", message.VideoIds)));

            Trace.WriteLine("The credit card values will be encrypted when looking at the messages in the queues");
            Trace.WriteLine(string.Format("CreditCard Number is {0}", message.EncryptedCreditCardNumber));
            Trace.WriteLine(string.Format("CreditCard Expiration Date is {0}", message.EncryptedExpirationDate));

            //tell the client that we received the order
            Bus.Publish(Bus.CreateInstance<OrderPlaced>(o =>
                {
                    o.ClientId = message.ClientId;
                    o.OrderNumber = message.OrderNumber;
                    o.VideoIds = message.VideoIds;
                }));
        }
    }
}