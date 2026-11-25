namespace EticaretMicroservice.Shared.Events
{
    public abstract record IntegrationEvent
    {
        public Guid CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
