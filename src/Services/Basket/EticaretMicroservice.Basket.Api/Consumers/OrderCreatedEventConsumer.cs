using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class OrderCreatedEventConsumer : IConsumer
{
    private readonly IStockService _stockService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger _logger;

    public OrderCreatedEventConsumer(
        IStockService stockService,
        IPublishEndpoint publishEndpoint,
        ILogger logger)
    {
        _stockService = stockService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Stock.API: OrderCreatedEvent alındı. OrderId: {OrderId}", message.OrderId);

        bool isStockAvailable = true;

        foreach (var item in message.OrderItems)
        {
            var hasStock = await _stockService.DecreaseStockAsync(item.ProductId, item.Quantity);
            if (!hasStock)
            {
                isStockAvailable = false;
                break;
            }
        }

        if (!isStockAvailable)
        {
            _logger.LogWarning("Yetersiz stok! OrderId: {OrderId} için StockFailedEvent fırlatılıyor.", message.OrderId);

            // ❌ Stok yetersiz! Telafi Event'i fırlatılıyor
            await _publishEndpoint.Publish(new StockFailedEvent
            {
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = "Yetersiz stok nedeniyle sipariş iptal edildi."
            });
        }
    }
}