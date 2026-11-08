using MassTransit;

namespace EticaretMicroservice.Services.Order.Application.Sagas;

// Bir siparişin süreç durumu (saga instance). Sipariş verisinin kendisi Orders tablosunda kalır;
// burada yalnızca sürecin yönetimi için gereken bilgiler tutulur.
public class OrderState : SagaStateMachineInstance
{
    // Order.CorrelationId ile aynı değer; Stock/Payment event'leri bu değerle eşleşir
    public Guid CorrelationId { get; set; }
    // Yeni instance'ta null olmalı: MassTransit null'ı "Initial" sayar, string.Empty'yi ise tanımsız durum adı
    // olarak okuyup UnknownStateException fırlatır. Kayıt anında her zaman dolu olduğu için kolon NOT NULL kalır.
    public string CurrentState { get; set; } = null!;

    public int OrderId { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Ödeme son tarihi. Zamanlanmış timeout mesajı kaybolursa (delayed exchange tek node'da tutulur)
    // süresi geçmiş ama hâlâ ödeme bekleyen instance'ları bulan bir emniyet kontrolü için kullanılır.
    public DateTime? ExpiresAt { get; set; }

    // Ödeme gelirse zamanlanmış timeout mesajını iptal edebilmek için
    public Guid? PaymentTimeoutTokenId { get; set; }

    // Yedek savunma: saga Pessimistic modda olduğu için birincil koruma satır kilididir (UPDLOCK) ve saga yolunda bu kontrol
    // başarısız olamaz. EF yine de her UPDATE'e "WHERE RowVersion = @okunan" ekler; kilit almadan OrderStates'e yazan
    // başka bir kod/elle düzeltme saga'nın yazısını sessizce ezemez.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
