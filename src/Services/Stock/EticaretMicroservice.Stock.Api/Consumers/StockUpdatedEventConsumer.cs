using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class StockUpdatedEventConsumer : IConsumer<StockUpdatedEvent>
{
    private readonly StockDbContext _context;
    private readonly ILogger<StockUpdatedEventConsumer> _logger;

    public StockUpdatedEventConsumer(StockDbContext context, ILogger<StockUpdatedEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockUpdatedEvent> context)
    {
        var message = context.Message;
        var stock = await _context.ProductStocks.FirstOrDefaultAsync(s => s.ProductId == message.ProductId);

        if (stock != null)
        {
            stock.AvailableStock = message.NewStock;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stok güncellendi. ProductId: {ProductId}, Yeni AvailableStock: {Stock}", message.ProductId, message.NewStock);
        }
        else
        {
            await _context.ProductStocks.AddAsync(new ProductStock
            {
                ProductId = message.ProductId,
                AvailableStock = message.NewStock,
                ReservedStock = 0
            });
            await _context.SaveChangesAsync();
            _logger.LogInformation("Yeni stok kaydı açıldı. ProductId: {ProductId}, AvailableStock: {Stock}", message.ProductId, message.NewStock);
        }
    }
}