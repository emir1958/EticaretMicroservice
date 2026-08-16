using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Shared.Events
{
    public record StockReservedEvent
    {
        public int OrderId { get; init; }
        public string BuyerId { get; init; }
        public decimal TotalPrice { get; init; }
        public PaymentMessage Payment { get; init; } = new();
        public List<OrderItemMessage> OrderItems { get; init; } = new(); // 👈 Eksikse ekleyin
    }

}
