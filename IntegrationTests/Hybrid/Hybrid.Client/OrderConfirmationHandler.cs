using System;
using Hybrid.Messages;
using NServiceBus;

namespace Hybrid.Client
{
    public class OrderConfirmationHandler : IHandleMessages<OrderConfirmed>
    {
        public IBus Bus { get; set; }

        public void Handle(OrderConfirmed message)
        {
            Console.WriteLine(@"Order for Product:{0} confirmed", message.Product);

        }
    }
}