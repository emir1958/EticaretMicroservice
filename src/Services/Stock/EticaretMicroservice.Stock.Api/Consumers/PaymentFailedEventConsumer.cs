using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly IStockService _stockService;
    private readonly StockDbContext _dbContext;
    private readonly ILogger<PaymentFailedEventConsumer> _logger;

    public PaymentFailedEventConsumer(
        IStockService stockService,
        StockDbContext dbContext,
        ILogger<PaymentFailedEventConsumer> logger)
    {
        _stockService = stockService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;
        _logger.LogWarning("[CorrelationId: {CorrelationId}] PaymentFailedEvent yakalandı! OrderId: {OrderId}. Rezerve stoklar iade ediliyor...",
            message.CorrelationId, message.OrderId);

        // Mesajdaki OrderItems'a değil, bu siparişin kayıtlı rezervasyonlarına göre iade edilir:
        // kopya mesaj ya da hiç rezerve edilmemiş bir sipariş başka siparişlerin stoğuna dokunamaz.
        var releasedCount = await _stockService.ReleaseForOrderAsync(message.OrderId, context.CancellationToken);

        if (releasedCount == 0)
        {
            _logger.LogInformation("[CorrelationId: {CorrelationId}] OrderId: {OrderId} için aktif rezervasyon yok (zaten iade/kesinleşmiş ya da hiç rezerve edilmemiş).",
                message.CorrelationId, message.OrderId);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
