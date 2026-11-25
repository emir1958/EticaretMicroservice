namespace EticaretMicroservice.Services.Order.Domain.Enums;

// Siparişin ödeme tarafı; OrderStatus'tan bağımsızdır (iptal edilmiş bir siparişin ödemesi alınıp iade edilmiş olabilir)
public enum PaymentStatus
{
    Alinmadi = 1,
    Alindi = 2,
    Basarisiz = 3,
    IadeEdildi = 4
}
