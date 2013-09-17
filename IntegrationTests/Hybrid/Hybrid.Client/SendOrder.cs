using Hybrid.Messages;
using NServiceBus;

namespace Hybrid.Client
{
    public class SendOrder : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }
        public void Start()
        {
            Bus.Send("Hybrid.Server", new PlaceOrder() { Product = "New shoes" });
        }
        public void Stop()
        {
        }
    }
}