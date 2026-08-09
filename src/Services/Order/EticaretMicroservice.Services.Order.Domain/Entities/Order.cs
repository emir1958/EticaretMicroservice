using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Services.Order.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Domain.Entities
{
    public class Order
    {
        public int Id { get; private set; }
        public string BuyerId { get; private set; } // JWT'den gelen ClaimTypes.NameIdentifier (UserId)
        public DateTime CreatedDate { get; private set; }
        public Address Address { get; private set; }

        // Encapsulation: Dışarıdan _orderItems.Add() yapılamaz, sadece sınıf içi metodla eklenir.
        private readonly List<OrderItem> _orderItems = new();
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
        public OrderStatus OrderStatus { get; private set; } = OrderStatus.Beklemede;

        private Order() { }

        public Order(string buyerId, Address address)
        {
            BuyerId = buyerId;
            Address = address;
            CreatedDate = DateTime.UtcNow;
        }

        // Domain Logic: Siparişe kalem ekleme metodu
        public void AddOrderItem(string productId, string productName, decimal price, int quantity)
        {
            var existingItem = _orderItems.FirstOrDefault(x => x.ProductId == productId);

            if (existingItem != null)
            {
                // Ürün zaten varsa adedini artır
                existingItem.UpdateOrderItem(productName, price, existingItem.Quantity + quantity);
            }
            else
            {
                _orderItems.Add(new OrderItem(productId, productName, price, quantity));
            }
        }

        // Toplam Tutarlık Hesaplama
        public decimal GetTotalPrice => _orderItems.Sum(x => x.Price * x.Quantity);
        public void SetStatusToCanceled()
        {
            OrderStatus = OrderStatus.IptalEdildi;
        }

        public void SetStatusToCompleted()
        {
            OrderStatus = OrderStatus.Tamamlandı;
        }
    }
}
