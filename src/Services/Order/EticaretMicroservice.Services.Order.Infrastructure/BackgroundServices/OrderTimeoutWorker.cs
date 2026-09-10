using EticaretMicroservice.Services.Order.Application.Hubs;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Infrastructure.BackgroundServices;

public class OrderTimeoutWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderTimeoutWorker> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan OrderTimeoutDuration = TimeSpan.FromMinutes(15);

    public OrderTimeoutWorker(IServiceProvider serviceProvider, ILogger<OrderTimeoutWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderTimeoutWorker devreye girdi. Kontrol aralığı: {Interval}", CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTimedOutOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zaman aşımına uğrayan siparişler taranırken beklenmedik hata oluştu.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task ProcessTimedOutOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<OrderHub>>();

        var threshold = DateTime.UtcNow.Subtract(OrderTimeoutDuration);
        var pendingOrders = await orderRepository.GetPendingOrdersOlderThanAsync(threshold, cancellationToken);

        if (pendingOrders.Count == 0)
            return;

        _logger.LogWarning("{Count} adet sipariş zaman aşımı kontrolüne girdi.", pendingOrders.Count);

        foreach (var order in pendingOrders)
        {
            // 🟢 1. Katı Durum Kuralı Kontrolü
            var canCancel = order.TrySetStatusToCanceled();
            if (!canCancel)
            {
                _logger.LogInformation("OrderId: {OrderId} zaten 'Beklemede' durumunda değil. Timeout iptali atlandı.", order.Id);
                continue;
            }

            try
            {
                // 🟢 2. Önce DB güncellemesini dene (RowVersion burada doğrulanır)
                await orderRepository.SaveChangesAsync(cancellationToken);

                // 🟢 3. DB'ye başarıyla yazıldıysa telafi event'ini fırlat
                var compensationEvent = new PaymentFailedEvent
                {
                    CorrelationId = Guid.NewGuid(),
                    OrderId = order.Id,
                    BuyerId = order.BuyerId,
                    Message = "Sipariş süresi doldu (15 dakika zaman aşımı).",
                    OrderItems = order.OrderItems.Select(x => new OrderItemMessage
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        Price = x.Price
                    }).ToList()
                };

                await publishEndpoint.Publish(compensationEvent, cancellationToken);

                await hubContext.Clients.Group(order.BuyerId).SendAsync("ReceiveOrderState", new
                {
                    OrderId = order.Id,
                    Status = "Canceled",
                    Message = "Siparişiniz zaman aşımı nedeniyle iptal edildi."
                }, cancellationToken);

                _logger.LogInformation("OrderId: {OrderId} zaman aşımı nedeniyle başarıyla iptal edildi ve stok iadesi tetiklendi.", order.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                // 🟢 Yarış durumu yakalandı: Tam bu esnada ödeme gelmiş ve satırı güncellemiş!
                _logger.LogWarning("OrderId: {OrderId} için Concurrency Conflict yakalandı. Ödeme işlemi zaman aşımıyla yarıştı ve ödeme kazandı. İptal işlemi geri çekildi.", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderId: {OrderId} zaman aşımına uğratılırken hata oluştu.", order.Id);
            }
        }
    }
}