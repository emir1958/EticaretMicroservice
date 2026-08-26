using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Shared.Events
{
    public record StockReservedEvent : IntegrationEvent
    {
        public int OrderId { get; init; }
        public string BuyerId { get; init; }
        public decimal TotalPrice { get; init; }
        public string PaymentToken { get; init; } = string.Empty;
        public List<OrderItemMessage> OrderItems { get; init; } = new(); // 👈 Eksikse ekleyin
    }

}
