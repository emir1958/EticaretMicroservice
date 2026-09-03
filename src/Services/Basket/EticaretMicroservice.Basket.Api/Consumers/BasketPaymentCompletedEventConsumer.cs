using EticaretMicroservice.Basket.Api.Services;
using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.Basket.Api.Consumers;

public class BasketPaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly IBasketService _basketService;
    private readonly ILogger<BasketPaymentCompletedEventConsumer> _logger;

    public BasketPaymentCompletedEventConsumer(
        IBasketService basketService,
        ILogger<BasketPaymentCompletedEventConsumer> logger)
    {
        _basketService = basketService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("[CorrelationId: {CorrelationId}] Basket.API: Ödeme onaylandı (OrderId: {OrderId}). BuyerId {BuyerId} için sepet temizleniyor...", 
            message.CorrelationId, message.OrderId, message.BuyerId);

        // Ödeme kesinleştiği için Redis'ten sepet silinir
        await _basketService.DeleteBasketAsync(message.BuyerId);
    }
}