namespace EticaretMicroservice.Shared.Commands;

// Order saga'sı tarafından, ödeme süresi dolan siparişin rezerve stoğunu iade etmek için gönderilir (compensation).
public record ReleaseStockCommand
{
    public Guid CorrelationId { get; init; }
    public int OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
