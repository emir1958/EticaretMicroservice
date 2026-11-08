namespace EticaretMicroservice.Payment.Api.Models
{
    public enum PaymentStatus
    {
        Success = 1,
        Failed = 2,
        Refunded = 3    // Sipariş iptal edildikten sonra alınan ödeme iade edildi (Order saga -> RefundPaymentCommand)
    }
}
