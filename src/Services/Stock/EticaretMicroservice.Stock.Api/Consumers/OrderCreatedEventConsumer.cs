using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IStockService _stockService;
    private readonly IPublishEndpoint _publishEndpoint; // 🔹 Event fırlatmak için eklendi
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
        _logger.LogInformation("Stock.API: OrderCreatedEvent yakalandı. OrderId: {OrderId}", message.OrderId);

        bool isAllStockAvailable = true;

        // 1. Tüm kalemler için stok var mı kontrol edelim / düşelim
        foreach (var item in message.OrderItems)
        {
            var isSuccess = await _stockService.DecreaseStockAsync(item.ProductId, item.Quantity);

            if (isSuccess)
            {
                _logger.LogInformation("Stok düşüldü -> ProductId: {ProductId}, Miktar: {Quantity}", item.ProductId, item.Quantity);
            }
            else
            {
                _logger.LogError("Stok yetersiz/başarısız! ProductId: {ProductId}", item.ProductId);
                isAllStockAvailable = false;
                break; // Bir ürün bile yoksa döngüden çıkıyoruz
            }
        }

        // 2. Stok durumuna göre bir sonraki Saga Adımını Tetikliyoruz
        if (isAllStockAvailable)
        {
            _logger.LogInformation("Tüm stoklar başarıyla rezerve edildi. OrderId: {OrderId}. Ödeme adımına geçiliyor...", message.OrderId);

            // 🟢 Stok Başarılı -> Payment.API için StockReservedEvent fırlatıyoruz
            await _publishEndpoint.Publish(new StockReservedEvent
            {
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                TotalPrice = message.OrderItems.Sum(x => x.Price * x.Quantity),
                Payment = message.Payment // (Gelecek kart bilgisi)
            });
        }
        else
        {
            _logger.LogWarning("Stok yetersiz olduğu için telafi süreci başlatılıyor! OrderId: {OrderId}", message.OrderId);

            // 🔴 Stok Başarısız -> Order.API siparişi iptal etsin diye StockFailedEvent fırlatıyoruz
            await _publishEndpoint.Publish(new StockFailedEvent
            {
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = "Stokta yeterli ürün bulunmamaktadır."
            });
        }
    }
}