using EticaretMicroservice.Basket.Api.Services;
using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.Basket.Api.Consumers;

public class BasketOrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IBasketService _basketService;
    private readonly ILogger<BasketOrderCreatedEventConsumer> _logger;

    public BasketOrderCreatedEventConsumer(
        IBasketService basketService,
        ILogger<BasketOrderCreatedEventConsumer> logger)
    {
        _basketService = basketService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var buyerId = context.Message.BuyerId;
        _logger.LogInformation("Basket.API: Sipariş oluşturuldu (OrderId: {OrderId}). Sepet temizleniyor...", context.Message.OrderId);

        // Redis'ten kullanıcının sepetini siliyoruz
        await _basketService.DeleteBasketAsync(buyerId);
    }
}