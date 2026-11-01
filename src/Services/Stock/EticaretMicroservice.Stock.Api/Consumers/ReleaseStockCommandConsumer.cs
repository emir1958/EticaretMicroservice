using EticaretMicroservice.Shared.Commands;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

// Order saga'sı, ödeme süresi dolan (veya iptalden sonra stok rezerve edilen) sipariş için gönderir.
public class ReleaseStockCommandConsumer : IConsumer<ReleaseStockCommand>
{
    private readonly IStockService _stockService;
    private readonly StockDbContext _dbContext;
    private readonly ILogger<ReleaseStockCommandConsumer> _logger;

    public ReleaseStockCommandConsumer(
        IStockService stockService,
        StockDbContext dbContext,
        ILogger<ReleaseStockCommandConsumer> logger)
    {
        _stockService = stockService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReleaseStockCommand> context)
    {
        var message = context.Message;
        _logger.LogWarning("[CorrelationId: {CorrelationId}] ReleaseStockCommand alındı. OrderId: {OrderId}, Sebep: {Reason}",
            message.CorrelationId, message.OrderId, message.Reason);

        // PaymentFailedEventConsumer ile aynı kural: yalnızca bu siparişin aktif rezervasyonları iade edilir, kopya istek zararsızdır.
        var releasedCount = await _stockService.ReleaseForOrderAsync(message.OrderId, context.CancellationToken);

        if (releasedCount == 0)
        {
            _logger.LogInformation("[CorrelationId: {CorrelationId}] OrderId: {OrderId} için aktif rezervasyon yok (zaten iade/kesinleşmiş ya da hiç rezerve edilmemiş).",
                message.CorrelationId, message.OrderId);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
