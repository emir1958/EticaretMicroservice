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

        // Sipariş sürecinin (saga) kimliği; Stock/Payment event'leri bu değeri taşır
        public Guid CorrelationId { get; private set; }
        public string BuyerId { get; private set; } // JWT'den gelen ClaimTypes.NameIdentifier (UserId)
        public DateTime CreatedDate { get; private set; }
        public Address Address { get; private set; }

        // Encapsulation: Dışarıdan _orderItems.Add() yapılamaz, sadece sınıf içi metodla eklenir.
        private readonly List<OrderItem> _orderItems = new();
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
        public OrderStatus OrderStatus { get; private set; } = OrderStatus.Beklemede;
        public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Alinmadi;
        public DateTime? RefundedAt { get; private set; }

        // Durum değiştiren her metot kendi kaydını ekler: durum ile geçmiş ayrışamaz
        private readonly List<OrderHistoryEntry> _history = new();
        public IReadOnlyCollection<OrderHistoryEntry> History => _history.AsReadOnly();

        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
        private Order() { }

        public Order(string buyerId, Address address, Guid correlationId)
        {
            if (correlationId == Guid.Empty)
                throw new ArgumentException("CorrelationId boş olamaz.", nameof(correlationId));

            CorrelationId = correlationId;
            BuyerId = buyerId;
            Address = address;
            CreatedDate = DateTime.UtcNow;
            AddHistory(OrderHistoryType.SiparisAlindi, CreatedDate);
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

        public bool TrySetStatusToCanceled(DateTime canceledAt, string? note = null)
        {
            if (OrderStatus != OrderStatus.Beklemede)
            {
                return false;
            }

            OrderStatus = OrderStatus.IptalEdildi;
            AddHistory(OrderHistoryType.IptalEdildi, canceledAt, note);
            return true;
        }

        public bool TrySetStatusToCompleted(DateTime completedAt)
        {
            if (OrderStatus != OrderStatus.Beklemede)
            {
                return false;
            }

            OrderStatus = OrderStatus.Tamamlandı;
            AddHistory(OrderHistoryType.Tamamlandi, completedAt);
            return true;
        }

        // Stok sonucu bir kez gelir; kopya event ikinci kayıt eklemez. Ödeme stoktan önce işlenmişse
        // sipariş tamamlanmış olabilir. İptalden sonra ayrılan stok hemen geri verildiği için geçmişe yazılmaz.
        public bool TryMarkStockReserved(DateTime reservedAt)
        {
            if (OrderStatus == OrderStatus.IptalEdildi || HasStockResult())
            {
                return false;
            }

            AddHistory(OrderHistoryType.StokAyrildi, reservedAt);
            return true;
        }

        public bool TryMarkStockFailed(DateTime failedAt, string? reason)
        {
            if (OrderStatus != OrderStatus.Beklemede || HasStockResult())
            {
                return false;
            }

            AddHistory(OrderHistoryType.StokYetersiz, failedAt, reason);
            return true;
        }

        public bool TryMarkPaymentReceived(DateTime receivedAt)
        {
            if (PaymentStatus != PaymentStatus.Alinmadi)
            {
                return false;
            }

            PaymentStatus = PaymentStatus.Alindi;
            AddHistory(OrderHistoryType.OdemeAlindi, receivedAt);
            return true;
        }

        public bool TryMarkPaymentFailed(DateTime failedAt, string? reason)
        {
            if (PaymentStatus != PaymentStatus.Alinmadi)
            {
                return false;
            }

            PaymentStatus = PaymentStatus.Basarisiz;
            AddHistory(OrderHistoryType.OdemeBasarisiz, failedAt, reason);
            return true;
        }

        // Yalnızca alınmış ödeme iade edilebilir; kopya iade bildirimi ilk tarihi değiştirmez
        public bool TryMarkRefunded(DateTime refundedAt)
        {
            if (PaymentStatus != PaymentStatus.Alindi)
            {
                return false;
            }

            PaymentStatus = PaymentStatus.IadeEdildi;
            RefundedAt = refundedAt;
            AddHistory(OrderHistoryType.IadeEdildi, refundedAt);
            return true;
        }

        private bool HasStockResult() =>
            _history.Any(h => h.Type is OrderHistoryType.StokAyrildi or OrderHistoryType.StokYetersiz);

        private void AddHistory(OrderHistoryType type, DateTime occurredAt, string? note = null) =>
            _history.Add(new OrderHistoryEntry(type, occurredAt, note));
    }
}
