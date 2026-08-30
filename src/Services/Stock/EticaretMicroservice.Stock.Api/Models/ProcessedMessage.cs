namespace EticaretMicroservice.Stock.Api.Models
{
    public class ProcessedMessage
    {
        public int Id { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
