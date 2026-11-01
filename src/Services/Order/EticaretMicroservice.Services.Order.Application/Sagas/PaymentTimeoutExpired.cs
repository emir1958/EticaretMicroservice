namespace EticaretMicroservice.Services.Order.Application.Sagas;

// Saga'nın kendine zamanladığı mesaj (Schedule). Yalnızca Order servisi içinde kullanıldığı için Shared'da değil.
public record PaymentTimeoutExpired
{
    public Guid CorrelationId { get; init; }
}
