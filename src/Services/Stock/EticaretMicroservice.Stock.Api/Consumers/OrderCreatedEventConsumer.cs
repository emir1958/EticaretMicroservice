using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Models;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IStockService _stockService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly StockDbContext _dbContext;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(
        IStockService stockService,
        IPublishEndpoint publishEndpoint,
        StockDbContext dbContext,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _stockService = stockService;
        _publishEndpoint = publishEndpoint;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        // 1. Idempotency Kontrolü
        var isAlreadyProcessed = await _dbContext.ProcessedMessages
            .AnyAsync(x => x.CorrelationId == message.CorrelationId);

        if (isAlreadyProcessed)
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] Bu mesaj daha önce işlendi, mükerrer işlem engellendi.", message.CorrelationId);
            return;
        }

        _logger.LogInformation("[CorrelationId: {CorrelationId}] Stock.API: OrderCreatedEvent yakalandı. OrderId: {OrderId}",
            message.CorrelationId, message.OrderId);

        bool isAllStockReserved = true;
        var reservedItems = new List<OrderItemMessage>();

        // 2. Stok Rezervasyonu (Atomik ve Geri Alınabilir Döngü)
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

        // 3. Kısmi Hata Durumunda Telafi (Rollback)
        if (!isAllStockReserved)
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] Kısmi stok hatası! Daha önce rezerve edilen {Count} kalem iade ediliyor...",
                message.CorrelationId, reservedItems.Count);

            foreach (var item in reservedItems)
            {
                await _stockService.ReleaseStockAsync(item.ProductId, item.Quantity);
            }

            await _publishEndpoint.Publish(new StockFailedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = "Siparişteki bir veya daha fazla ürün için yeterli stok bulunamadı."
            });

            return;
        }

        // 4. Tüm Rezervasyonlar Başarılıysa Mesajı İşlendi Olarak Kaydet ve Devam Et
        _dbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            CorrelationId = message.CorrelationId,
            ProcessedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("[CorrelationId: {CorrelationId}] Tüm ürünler başarıyla rezerve edildi. OrderId: {OrderId}. Ödeme adımına geçiliyor.",
            message.CorrelationId, message.OrderId);

        await _publishEndpoint.Publish(new StockReservedEvent
        {
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            BuyerId = message.BuyerId,
            TotalPrice = message.OrderItems.Sum(x => x.Price * x.Quantity),
            PaymentToken = message.PaymentToken,
            OrderItems = message.OrderItems
        });
    }
}