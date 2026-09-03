using EticaretMicroservice.Services.Order.Application.Hubs;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
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
        _logger.LogInformation("OrderTimeoutWorker devreye girdi. Zaman aşımı kontrolü: {Interval} aralıkla çalışıyor.", CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTimedOutOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zaman aşımına uğrayan siparişler kontrol edilirken bir hata meydana geldi.");
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

        _logger.LogWarning("{Count} adet sipariş zaman aşımına uğradı (Timeout). İptal ve stok telafi süreci başlatılıyor...", pendingOrders.Count);

        foreach (var order in pendingOrders)
        {
            // 1. Siparişi iptal et
            order.SetStatusToCanceled();

            // 2. Stock API'nin rezerve edilen stoğu serbest bırakması için telafi event'i yayınla
            var compensationEvent = new PaymentFailedEvent
            {
                CorrelationId = Guid.NewGuid(),
                OrderId = order.Id,
                BuyerId = order.BuyerId,
                Message = "Sipariş süresi doldu (Ödeme zaman aşımı: 15 dakika).",
                OrderItems = order.OrderItems.Select(x => new OrderItemMessage
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    Price = x.Price
                }).ToList()
            };

            await publishEndpoint.Publish(compensationEvent, cancellationToken);

            // 3. SignalR ile kullanıcıya bildirim ilet
            await hubContext.Clients.Group(order.BuyerId).SendAsync("ReceiveOrderState", new
            {
                OrderId = order.Id,
                Status = "Canceled",
                Message = "Sipariş işlemi zaman aşımına uğradı ve iptal edildi."
            }, cancellationToken);

            _logger.LogInformation("Zaman aşımı nedeniyle sipariş iptal edildi. OrderId: {OrderId}", order.Id);
        }

        await orderRepository.SaveChangesAsync(cancellationToken);
    }
}