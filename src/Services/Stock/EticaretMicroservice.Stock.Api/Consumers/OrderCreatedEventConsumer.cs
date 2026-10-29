using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IStockService _stockService;
    private readonly StockDbContext _dbContext;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(
        IStockService stockService,
        StockDbContext dbContext,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _stockService = stockService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("[CorrelationId: {CorrelationId}] Stock.API: OrderCreatedEvent yakalandı. OrderId: {OrderId}",
            message.CorrelationId, message.OrderId);

        var result = await _stockService.ReserveForOrderAsync(message.OrderId, message.OrderItems, context.CancellationToken);

        switch (result)
        {
            case ReservationResult.AlreadyProcessed:
                // İlk işlemin sonucu (StockReserved/StockFailed) aynı transaction'da outbox'a yazıldığı için
                // zaten yayınlandı ya da yayınlanacak; tekrar publish edilmez.
                _logger.LogWarning("[CorrelationId: {CorrelationId}] OrderId: {OrderId} daha önce işlenmiş, kopya mesaj atlandı.",
                    message.CorrelationId, message.OrderId);
                return;

            case ReservationResult.InsufficientStock:
                await context.Publish(new StockFailedEvent
                {
                    CorrelationId = message.CorrelationId,
                    OrderId = message.OrderId,
                    BuyerId = message.BuyerId,
                    Message = "Siparişteki bir veya daha fazla ürün için yeterli stok bulunamadı."
                });
                break;

            case ReservationResult.Reserved:
                await context.Publish(new StockReservedEvent
                {
                    CorrelationId = message.CorrelationId,
                    OrderId = message.OrderId,
                    BuyerId = message.BuyerId,
                    TotalPrice = message.OrderItems.Sum(x => x.Price * x.Quantity),
                    PaymentToken = message.PaymentToken,
                    OrderItems = message.OrderItems
                });
                break;
        }

        // Rezervasyon kayıtları + outbox mesajı; EF Outbox transaction'ı commit edildiğinde birlikte kalıcı olur
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("[CorrelationId: {CorrelationId}] OrderId: {OrderId} stok sonucu: {Result}",
            message.CorrelationId, message.OrderId, result);
    }
}
