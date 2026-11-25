namespace EticaretMicroservice.Shared.Events
{
    public record StockFailedEvent : IntegrationEvent
    {
        public int OrderId { get; init; }
        public string BuyerId { get; init; }
        public string Message { get; init; }
    }
}
