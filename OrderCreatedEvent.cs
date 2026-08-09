using System.Collections.Generic;

namespace EticaretMicroservice.Shared.Events
{
    // Event'ler immutable (değiştirilemez) olmalıdır. 
    public record OrderCreatedEvent
    {
        public int OrderId { get; init; }
        public string BuyerId { get; init; }
        public List<OrderItemMessage> OrderItems { get; init; } = new();
    }

    public record OrderItemMessage
    {
        public string ProductId { get; init; }
        public int Quantity { get; init; }
        public decimal Price { get; init; }
    }
}