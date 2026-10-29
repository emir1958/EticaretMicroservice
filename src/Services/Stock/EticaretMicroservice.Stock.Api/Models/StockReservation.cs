namespace EticaretMicroservice.Stock.Api.Models
{
    public class StockReservation
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public StockReservationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public enum StockReservationStatus
    {
        Reserved = 1,   // Stok ayrıldı, ödeme bekleniyor
        Confirmed = 2,  // Ödeme alındı, satış kesinleşti
        Released = 3,   // Ödeme başarısız / zaman aşımı, stok iade edildi
        Rejected = 4    // Yetersiz stok nedeniyle sipariş reddedildi (karar kalıcı olsun diye kaydedilir)
    }
}
