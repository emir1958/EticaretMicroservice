using EticaretMicroservice.Stock.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Services;

public interface IStockService
{
    Task<bool> DecreaseStockAsync(string productId, int quantity);
}

public class StockService : IStockService
{
    private readonly StockDbContext _context;
    private readonly ILogger<StockService> _logger;
    public StockService(StockDbContext context, ILogger<StockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> DecreaseStockAsync(string productId, int quantity)
    {
        var stock = await _context.ProductStocks.FirstOrDefaultAsync(x => x.ProductId == productId);

        if (stock == null)
        {
            _logger.LogWarning("Ürün bulunamadı: {ProductId}", productId);
            return false;
        }

        if (stock.AvailableStock < quantity)
        {
            _logger.LogWarning("Yetersiz stok! Ürün: {ProductId}, Mevcut: {Available}, İstenen: {Quantity}",
                productId, stock.AvailableStock, quantity);
            return false;
        }

        stock.AvailableStock -= quantity;
        await _context.SaveChangesAsync();

        return true;
    }
}