namespace EticaretMicroservice.Shared.Events;

// Payment, iadeyi sağlayıcıda tamamladıktan sonra yayınlar. İade zamanı base class'taki CreatedAt'tir.
public record PaymentRefundedEvent : IntegrationEvent
{
    public int OrderId { get; init; }
    public decimal Amount { get; init; }
}
