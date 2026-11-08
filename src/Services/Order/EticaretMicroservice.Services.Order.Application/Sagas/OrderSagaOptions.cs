namespace EticaretMicroservice.Services.Order.Application.Sagas;

// "OrderSaga" konfigürasyon bölümü
public class OrderSagaOptions
{
    public const string SectionName = "OrderSaga";

    // Sipariş oluşturulduktan sonra ödemenin tamamlanması için tanınan süre
    public TimeSpan PaymentTimeout { get; set; } = TimeSpan.FromMinutes(15);

    // OrderTimeoutSweeper: kaçan timeout mesajları için tarama aralığı
    public TimeSpan SweeperInterval { get; set; } = TimeSpan.FromMinutes(1);

    // ExpiresAt'ten sonra zamanlanmış mesaja tanınan pay; sweeper yalnızca bu süreyi de geçmiş instance'lara dokunur
    public TimeSpan SweeperGrace { get; set; } = TimeSpan.FromMinutes(2);
}
