namespace EticaretMicroservice.Shared.Events
{
    public record PaymentFailedEvent : IntegrationEvent
    {
        public int OrderId { get; init; }
        public string BuyerId { get; init; }
        public string Message { get; init; } = string.Empty;
        public List<OrderItemMessage> OrderItems { get; init; } = new();
    }
}
