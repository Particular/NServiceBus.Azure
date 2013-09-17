using NServiceBus;

namespace Hybrid.Messages
{
    public class OrderConfirmed : IEvent
    {
        public string OrderId { get; set; }
        public string Product { get; set; }
    }
}