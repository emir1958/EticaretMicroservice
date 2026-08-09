using EticaretMicroservice.Shared.Events;

namespace EticaretMicroservice.Payment.Api.Services;

public interface IPaymentService
{
    (bool IsSuccess, string FailReason) ProcessPayment(PaymentMessage payment, decimal totalPrice);
}

public class FakePaymentService : IPaymentService
{
    public (bool IsSuccess, string FailReason) ProcessPayment(PaymentMessage payment, decimal totalPrice)
    {
        // 1. Kart Numarası Kontrolü (16 Hane mi?)
        var cleanCardNumber = payment.CardNumber?.Replace(" ", "").Replace("-", "") ?? "";
        if (string.IsNullOrWhiteSpace(cleanCardNumber) || cleanCardNumber.Length != 16)
        {
            return (false, "Geçersiz Kredi Kartı Numarası! 16 haneli olmalıdır.");
        }

        // 2. CVC Kontrolü (3 Hane mi?)
        if (string.IsNullOrWhiteSpace(payment.Cvc) || payment.Cvc.Length != 3)
        {
            return (false, "Geçersiz CVC / Güvenlik Kodu!");
        }

        // 3. Gerçek Sanal POS Test Kuralı (Kart No Sonu "0000" ise Banka Bakiye Yetersiz Desin)
        if (cleanCardNumber.EndsWith("0000"))
        {
            return (false, "Kart Bakiyesi / Limiti Yetersiz (Banka Reddi: Err-1002).");
        }

        // 4. Gerçek Sanal POS Test Kuralı (Kart No Sonu "9999" ise İnternet Alışverişine Kapalı Desin)
        if (cleanCardNumber.EndsWith("9999"))
        {
            return (false, "Kartınız İnternet Alışverişine Kapalıdır (Banka Reddi: Err-1008).");
        }

        // Tüm kontrollerden geçtiyse ödeme başarılı
        return (true, string.Empty);
    }
}