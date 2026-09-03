using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Services;

public interface IStockService
{
    Task<bool> ReserveStockAsync(string productId, int quantity);
    Task<bool> ReleaseStockAsync(string productId, int quantity);
    Task<bool> CreateStockAsync(string productId, int initialStock);
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

    // 🟢 1. Stok Rezerve Etme (Hold) - Ödeme Öncesi Akış
    public async Task<bool> ReserveStockAsync(string productId, int quantity)
    {
        var stock = await _context.ProductStocks.FirstOrDefaultAsync(x => x.ProductId == productId);

        if (stock == null)
        {
            _logger.LogWarning("Ürün stoğu bulunamadı: {ProductId}", productId);
            return false;
        }

        if (stock.AvailableStock < quantity)
        {
            _logger.LogWarning("Yetersiz stok! Ürün: {ProductId}, Mevcut: {Available}, İstenen: {Quantity}",
                productId, stock.AvailableStock, quantity);
            return false;
        }

        // Stok rezervasyonu yapılır
        stock.AvailableStock -= quantity;
        stock.ReservedStock += quantity;

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Stok rezervasyon çakışması (Concurrency Conflict)! ProductId: {ProductId}", productId);
            _context.Entry(stock).State = EntityState.Detached;
            return false;
        }
    }

    // 🟢 2. Stok Rezervasyon İadesi (Release) - Ödeme Başarısız Olunca
    public async Task<bool> ReleaseStockAsync(string productId, int quantity)
    {
        var stock = await _context.ProductStocks.FirstOrDefaultAsync(x => x.ProductId == productId);

        if (stock == null)
        {
            _logger.LogWarning("Stok iadesi yapılamadı. Ürün bulunamadı: {ProductId}", productId);
            return false;
        }

        stock.AvailableStock += quantity;
        if (stock.ReservedStock >= quantity)
        {
            stock.ReservedStock -= quantity;
        }

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stok rezervasyonu serbest bırakıldı -> ProductId: {ProductId}, Miktar: {Quantity}", productId, quantity);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Stok serbest bırakma çakışması (Concurrency Conflict)! ProductId: {ProductId}", productId);
            _context.Entry(stock).State = EntityState.Detached;
            return false;
        }
    }

    // 🟢 3. Yeni Ürün İçin Stok Oluşturma (Catalog API Senkronu İçin)
    public async Task<bool> CreateStockAsync(string productId, int initialStock)
    {
        var exists = await _context.ProductStocks.AnyAsync(x => x.ProductId == productId);
        if (exists) return true;

        var newStock = new ProductStock
        {
            ProductId = productId,
            AvailableStock = initialStock,
            ReservedStock = 0
        };

        await _context.ProductStocks.AddAsync(newStock);
        await _context.SaveChangesAsync();
        return true;
    }
}