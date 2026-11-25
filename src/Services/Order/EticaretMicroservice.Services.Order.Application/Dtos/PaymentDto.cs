namespace EticaretMicroservice.Services.Order.Application.Dtos
{
    public class PaymentDto
    {
        public string? PaymentToken { get; set; }
        public string CardName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string Expiration { get; set; } = string.Empty;
        public string Cvc { get; set; } = string.Empty;
    }
}
