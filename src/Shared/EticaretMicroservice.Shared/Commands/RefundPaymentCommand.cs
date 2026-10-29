namespace EticaretMicroservice.Shared.Commands;

// Order saga'sı tarafından, iptal edilmiş bir sipariş için ödeme alındığında gönderilir (compensation).
public record RefundPaymentCommand
{
    public Guid CorrelationId { get; init; }
    public int OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
