using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IStockService _stockService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(
        IStockService stockService,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _stockService = stockService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        // 🟢 1. LOGLARA CorrelationId DAHİL EDİLDİ
        _logger.LogInformation("[CorrelationId: {CorrelationId}] Stock.API: OrderCreatedEvent yakalandı. OrderId: {OrderId}",
            message.CorrelationId, message.OrderId);

        bool isAllStockAvailable = true;

        // 1. Tüm kalemler için stok kontrolü ve düşümü
        foreach (var item in message.OrderItems)
        {
            var isSuccess = await _stockService.DecreaseStockAsync(item.ProductId, item.Quantity);

            if (isSuccess)
            {
                _logger.LogInformation("[CorrelationId: {CorrelationId}] Stok düşüldü -> ProductId: {ProductId}, Miktar: {Quantity}",
                    message.CorrelationId, item.ProductId, item.Quantity);
            }
            else
            {
                _logger.LogError("[CorrelationId: {CorrelationId}] Stok yetersiz/başarısız! ProductId: {ProductId}",
                    message.CorrelationId, item.ProductId);
                isAllStockAvailable = false;
                break;
            }
        }

        // 2. Akışın devamını tetikleme
        if (isAllStockAvailable)
        {
            _logger.LogInformation("[CorrelationId: {CorrelationId}] Tüm stoklar başarıyla rezerve edildi. OrderId: {OrderId}. Ödeme adımına geçiliyor...",
                message.CorrelationId, message.OrderId);

            // 🟢 2. StockReservedEvent İÇİNE CorrelationId AKTARILDI
            await _publishEndpoint.Publish(new StockReservedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                TotalPrice = message.OrderItems.Sum(x => x.Price * x.Quantity),
                PaymentToken = message.PaymentToken,
                OrderItems = message.OrderItems
            });
        }
        else
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] Stok yetersiz olduğu için telafi süreci başlatılıyor! OrderId: {OrderId}",
                message.CorrelationId, message.OrderId);

            // 🔴 3. StockFailedEvent İÇİNE CorrelationId AKTARILDI
            await _publishEndpoint.Publish(new StockFailedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = "Stokta yeterli ürün bulunmamaktadır."
            });
        }
    }
}