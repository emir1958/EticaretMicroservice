using EticaretMicroservice.Stock.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Services;

public interface IStockService
{
    Task<bool> DecreaseStockAsync(string productId, int quantity);
    Task<bool> IncreaseStockAsync(string productId, int quantity);
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

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // 🟢 ÇAKIŞMA YAKALANDI: Başka bir işlem bu stok satırını aynı anda güncelledi!
            _logger.LogError(ex, "Stok düşüm çakışması (Concurrency Conflict)! ProductId: {ProductId}", productId);

            // EF ChangeTracker'ı temizliyoruz
            _context.Entry(stock).State = EntityState.Detached;
            return false;
        }
    }

    // 🟢 Stok İade Metodu (Concurrency Korumalı)
    public async Task<bool> IncreaseStockAsync(string productId, int quantity)
    {
        var stock = await _context.ProductStocks.FirstOrDefaultAsync(x => x.ProductId == productId);

        if (stock == null)
        {
            _logger.LogWarning("Stok iadesi yapılamadı. Ürün bulunamadı: {ProductId}", productId);
            return false;
        }

        stock.AvailableStock += quantity;

        try
        {
            await _context.SaveChangesAsync();

            _logger.LogInformation("Stok iade edildi -> ProductId: {ProductId}, Eklenen Miktar: {Quantity}, Yeni Stok: {Available}",
                productId, quantity, stock.AvailableStock);

            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Stok iade çakışması (Concurrency Conflict)! ProductId: {ProductId}", productId);
            _context.Entry(stock).State = EntityState.Detached;
            return false;
        }
    }
}