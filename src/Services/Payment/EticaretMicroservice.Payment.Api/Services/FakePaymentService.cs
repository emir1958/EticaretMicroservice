namespace EticaretMicroservice.Payment.Api.Services;

public interface IPaymentService
{
    (bool IsSuccess, string FailReason) ProcessPayment(string paymentToken, decimal totalPrice);

    // Gerçek bir sağlayıcıda OrderId idempotency anahtarı olarak gönderilmeli: DB commit'i başarısız olup mesaj
    // yeniden işlenirse aynı iade ikinci kez yapılmamalı.
    (bool IsSuccess, string FailReason) RefundPayment(int orderId, string paymentToken, decimal amount);
}

public class FakePaymentService : IPaymentService
{
    public (bool IsSuccess, string FailReason) ProcessPayment(string paymentToken, decimal totalPrice)
    {
        if (string.IsNullOrWhiteSpace(paymentToken))
        {
            return (false, "Geçersiz veya eksik ödeme tokenı!");
        }

        // Test kartları: token 0000 ile biterse bakiye yetersiz, 9999 ile biterse internete kapalı
        if (paymentToken.EndsWith("0000"))
        {
            return (false, "Kart Bakiyesi / Limiti Yetersiz (Banka Reddi: Err-1002).");
        }

        if (paymentToken.EndsWith("9999"))
        {
            return (false, "Kartınız İnternet Alışverişine Kapalıdır (Banka Reddi: Err-1008).");
        }

        if (totalPrice <= 0)
        {
            return (false, "Ödeme tutarı geçersiz!");
        }

        return (true, string.Empty);
    }

    // Simülasyon: daha önce başarıyla çekilmiş bir ödemenin iadesi her zaman kabul edilir
    public (bool IsSuccess, string FailReason) RefundPayment(int orderId, string paymentToken, decimal amount)
    {
        if (amount <= 0)
        {
            return (false, "İade tutarı geçersiz!");
        }

        return (true, string.Empty);
    }
}