namespace EticaretMicroservice.Services.Order.Application.Sagas;

// "OrderSaga" konfigürasyon bölümü
public class OrderSagaOptions
{
    public const string SectionName = "OrderSaga";

    // Sipariş oluşturulduktan sonra ödemenin tamamlanması için tanınan süre
    public TimeSpan PaymentTimeout { get; set; } = TimeSpan.FromMinutes(15);
}
