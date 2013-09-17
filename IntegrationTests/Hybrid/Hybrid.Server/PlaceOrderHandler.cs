using System;
using Hybrid.Messages;
using NServiceBus;

namespace Hybrid.Server
{
    public class PlaceOrderHandler : IHandleMessages<PlaceOrder>
    {
        public IBus Bus { get; set; }

        public void Handle(PlaceOrder message)
        {
            Console.WriteLine(@"Order for Product:{0} placed", message.Product);

            Bus.Publish(new OrderConfirmed
            {
                OrderId = Guid.NewGuid().ToString(),
                Product = message.Product
            });
        }
    }
}