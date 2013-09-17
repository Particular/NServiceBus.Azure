using NServiceBus;

namespace Hybrid.Messages
{
    public class PlaceOrder : ICommand
    {
        public string Product { get; set; }
    }
}
