using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

// 1. Generic IConsumer<OrderCreatedEvent> eklendi
public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IStockService _stockService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderCreatedEventConsumer> _logger; // 2. Generic ILogger eklendi

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

        if (isStockAvailable)
        {
            _logger.LogInformation("Tüm stoklar başarıyla düşüldü. OrderId: {OrderId}. Payment.API için StockReservedEvent fırlatılıyor...", message.OrderId);

            // 🟢 STOK BAŞARILI: Ödeme adımına geçmek için StockReservedEvent yayımlıyoruz
            await _publishEndpoint.Publish(new StockReservedEvent
            {
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                TotalPrice = message.OrderItems.Sum(x => x.Price * x.Quantity),
                Payment = message.Payment,
                OrderItems = message.OrderItems // Ödeme hatası durumunda stok iadesi için gerekli
            });
        }
        else
        {
            _logger.LogWarning("Yetersiz stok! OrderId: {OrderId} için StockFailedEvent fırlatılıyor.", message.OrderId);

            // 🔴 STOK YETERSİZ: Siparişi iptal etmek için StockFailedEvent yayımlıyoruz
            await _publishEndpoint.Publish(new StockFailedEvent
            {
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = "Yetersiz stok nedeniyle sipariş iptal edildi."
            });
        }
    }
}