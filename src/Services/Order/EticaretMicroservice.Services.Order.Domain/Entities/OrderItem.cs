using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; private set; }
        public string ProductId { get; private set; }
        public string ProductName { get; private set; }
        public decimal Price { get; private set; }
        public int Quantity { get; private set; }

        // Entity Framework Core için parametresiz constructor
        private OrderItem() { }

        public OrderItem(string productId, string productName, decimal price, int quantity)
        {
            ProductId = productId;
            ProductName = productName;
            Price = price;
            Quantity = quantity;
        }

        // Fiyat veya Adet güncelleme gibi domain logic'ler buraya yazılır
        public void UpdateOrderItem(string productName, decimal price, int quantity)
        {
            ProductName = productName;
            Price = price;
            Quantity = quantity;
        }
    }
}
