namespace EticaretMicroservice.Services.Order.Application.Interfaces;

// Sipariş durumu değişikliklerini kullanıcıya iletir. Application katmanı bildirimin
// nasıl taşındığını (SignalR, e-posta...) bilmez; implementasyon Infrastructure'dadır.
public interface IOrderNotifier
{
    Task NotifyOrderStateAsync(string buyerId, int orderId, string status, string message, CancellationToken cancellationToken = default);
}
