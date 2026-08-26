using System;
using System.Collections.Generic;

namespace EticaretMicroservice.Shared.Events
{
    public record OrderCreatedEvent
    {
        public int OrderId { get; init; }
        public string BuyerId { get; init; }
        public List<OrderItemMessage> OrderItems { get; init; } = new();

        public string PaymentToken { get; init; } = string.Empty;
    }

    public record OrderItemMessage
    {
        public string ProductId { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal Price { get; init; }
    }
 
}