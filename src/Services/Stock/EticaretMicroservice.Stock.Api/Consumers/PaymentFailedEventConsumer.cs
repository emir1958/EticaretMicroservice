using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly IStockService _stockService;
    private readonly ILogger<PaymentFailedEventConsumer> _logger;

    public PaymentFailedEventConsumer(IStockService stockService, ILogger<PaymentFailedEventConsumer> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;
        _logger.LogWarning("PaymentFailedEvent yakalandı! OrderId: {OrderId}. Düşülen stoklar geri iade ediliyor...", message.OrderId);

        foreach (var item in message.OrderItems)
        {
            // Stok miktarını artırarak geri iade ediyoruz (IncreaseStockAsync servisi gereklidir)
            await _stockService.IncreaseStockAsync(item.ProductId, item.Quantity);
            _logger.LogInformation("Stok iade edildi -> ProductId: {ProductId}, Miktar: {Quantity}", item.ProductId, item.Quantity);
        }
    }
}