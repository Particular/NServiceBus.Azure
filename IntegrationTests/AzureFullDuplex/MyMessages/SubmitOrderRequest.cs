using System;

namespace MyMessages
{
    public interface SubmitOrderRequest : IDefineMessages
    {
        Guid Id { get; set; }
        int Quantity { get; set; }
    }
}
