using System;
using System.Collections.Generic;

namespace EticaretMicroservice.Shared.Events
{
    public record OrderCreatedEvent
    {
        public int OrderId { get; init; }
        public string BuyerId { get; init; }
        public List<OrderItemMessage> OrderItems { get; init; } = new();

        // 🔹 CS1061 Hatasını Çözen Yeni Eklenen Alan:
        public PaymentMessage Payment { get; init; } = new();
    }

    public record OrderItemMessage
    {
        public string ProductId { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal Price { get; init; }
    }

    // 🔹 Payment Bilgisi İçin Shared Model:
    public record PaymentMessage
    {
        public string CardName { get; init; } = string.Empty;
        public string CardNumber { get; init; } = string.Empty;
        public string Expiration { get; init; } = string.Empty;
        public string Cvc { get; init; } = string.Empty;
    }
}