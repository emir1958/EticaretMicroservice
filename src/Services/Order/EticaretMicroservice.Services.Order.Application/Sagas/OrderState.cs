using MassTransit;

namespace EticaretMicroservice.Services.Order.Application.Sagas;

// Bir siparişin süreç durumu (saga instance). Sipariş verisinin kendisi Orders tablosunda kalır;
// burada yalnızca sürecin yönetimi için gereken bilgiler tutulur.
public class OrderState : SagaStateMachineInstance
{
    // Order.CorrelationId ile aynı değer; Stock/Payment event'leri bu değerle eşleşir
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public int OrderId { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Ödeme son tarihi. Zamanlanmış timeout mesajı kaybolursa (delayed exchange tek node'da tutulur)
    // süresi geçmiş ama hâlâ ödeme bekleyen instance'ları bulan bir emniyet kontrolü için kullanılır.
    public DateTime? ExpiresAt { get; set; }

    // Ödeme gelirse zamanlanmış timeout mesajını iptal edebilmek için
    public Guid? PaymentTimeoutTokenId { get; set; }

    // Optimistic concurrency: aynı instance'a eşzamanlı iki event yazamaz
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
