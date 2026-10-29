using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class StockPaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly IStockService _stockService;
    private readonly StockDbContext _dbContext;
    private readonly ILogger<StockPaymentCompletedEventConsumer> _logger;

    public StockPaymentCompletedEventConsumer(
        IStockService stockService,
        StockDbContext dbContext,
        ILogger<StockPaymentCompletedEventConsumer> logger)
    {
        _stockService = stockService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("[CorrelationId: {CorrelationId}] Stock.API: PaymentCompletedEvent alındı. OrderId: {OrderId}. Rezervasyon kesinleştiriliyor...",
            message.CorrelationId, message.OrderId);

        var confirmedCount = await _stockService.ConfirmForOrderAsync(message.OrderId, context.CancellationToken);

        if (confirmedCount == 0)
        {
            // Örn. sipariş zaman aşımıyla iade edildikten sonra ödeme geldiyse: stok tarafında yapılacak bir şey yok,
            // ancak para çekilmiş olabilir. Bu durumun telafisi (refund) Order saga'sının sorumluluğudur.
            _logger.LogWarning("[CorrelationId: {CorrelationId}] OrderId: {OrderId} için kesinleştirilecek aktif rezervasyon bulunamadı.",
                message.CorrelationId, message.OrderId);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
