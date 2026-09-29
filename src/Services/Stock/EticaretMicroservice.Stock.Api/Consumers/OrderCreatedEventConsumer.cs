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

        // ❌ SİLİNDİ: Manuel ProcessedMessages AnyAsync kontrolü

        _logger.LogInformation("[CorrelationId: {CorrelationId}] Stock.API: OrderCreatedEvent yakalandı. OrderId: {OrderId}",
            message.CorrelationId, message.OrderId);

        bool isAllStockReserved = true;
        var reservedItems = new List<OrderItemMessage>();

        // Stok Rezervasyonu
        foreach (var item in message.OrderItems)
        {
            var isReserved = await _stockService.ReserveStockAsync(item.ProductId, item.Quantity);

            if (isReserved)
            {
                reservedItems.Add(item);
                _logger.LogInformation("[CorrelationId: {CorrelationId}] Stok rezerve edildi -> ProductId: {ProductId}, Miktar: {Quantity}",
                    message.CorrelationId, item.ProductId, item.Quantity);
            }
            else
            {
                _logger.LogError("[CorrelationId: {CorrelationId}] Yetersiz stok veya çakışma! ProductId: {ProductId}",
                    message.CorrelationId, item.ProductId);
                isAllStockReserved = false;
                break;
            }
        }

        // Kısmi Hata Durumunda Telafi (Rollback)
        if (!isAllStockReserved)
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] Kısmi stok hatası! Daha önce rezerve edilen {Count} kalem iade ediliyor...",
                message.CorrelationId, reservedItems.Count);

            foreach (var item in reservedItems)
            {
                await _stockService.ReleaseStockAsync(item.ProductId, item.Quantity);
            }

            await context.Publish(new StockFailedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = "Siparişteki bir veya daha fazla ürün için yeterli stok bulunamadı."
            });

            await _dbContext.SaveChangesAsync();
            return;
        }

        // ❌ SİLİNDİ: _dbContext.ProcessedMessages.Add(...) satırları

        // Tüm Rezervasyonlar Başarılıysa
        await context.Publish(new StockReservedEvent
        {
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            BuyerId = message.BuyerId,
            TotalPrice = message.OrderItems.Sum(x => x.Price * x.Quantity),
            PaymentToken = message.PaymentToken,
            OrderItems = message.OrderItems
        });

        // 🟢 Tek bir SaveChanges çağrısı ile; 
        // 1. Stok güncellemeleri (ProductStocks)
        // 2. MassTransit Inbox kaydı (Mükerrer koruma)
        // 3. MassTransit Outbox kaydı (StockReservedEvent)
        // atomik olarak veritabanına işlenir. Biri başarısız olursa tümü iptal olur.
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("[CorrelationId: {CorrelationId}] Tüm ürünler başarıyla rezerve edildi. OrderId: {OrderId}. Ödeme adımına geçiliyor.",
            message.CorrelationId, message.OrderId);
    }
}