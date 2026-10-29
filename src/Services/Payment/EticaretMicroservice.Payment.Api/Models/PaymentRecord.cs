namespace EticaretMicroservice.Payment.Api.Models
{
    public class PaymentRecord
    {
        public int Id { get; set; }
        public Guid CorrelationId { get; set; }
        public int OrderId { get; set; }
        public string BuyerId { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string PaymentToken { get; set; } = string.Empty;

        public PaymentStatus Status { get; set; }
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
