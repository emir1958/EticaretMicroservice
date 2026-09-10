using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Services;

public interface IStockService
{
    Task<bool> ReserveStockAsync(string productId, int quantity);
    Task<bool> ReleaseStockAsync(string productId, int quantity);
    Task<bool> ConfirmReservationAsync(string productId, int quantity); // 🟢 Ödeme başarılıysa rezerveyi kalıcı düşer
    Task<bool> IncreaseStockAsync(string productId, int quantity);       // 🟢 Eski/uyumsuz çağrılar için güvenli alias
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

    // 1. Stok Rezerve Etme (Hold) - Sipariş ilk oluştuğunda
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

    // 2. Stok Rezervasyon İadesi (Release) - Ödeme başarısız olduğunda veya zaman aşımında
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
        else
        {
            stock.ReservedStock = 0;
        }

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stok rezervasyonu serbest bırakıldı -> ProductId: {ProductId}, Miktar: {Quantity}", productId, quantity);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Stok serbest bırakma çakışması! ProductId: {ProductId}", productId);
            _context.Entry(stock).State = EntityState.Detached;
            return false;
        }
    }

    // 🟢 3. Rezervasyonu Onaylama (Commit) - Ödeme başarılı olduğunda çağrılır
    public async Task<bool> ConfirmReservationAsync(string productId, int quantity)
    {
        var stock = await _context.ProductStocks.FirstOrDefaultAsync(x => x.ProductId == productId);

        if (stock == null)
        {
            _logger.LogWarning("Rezervasyon onaylanamadı. Ürün bulunamadı: {ProductId}", productId);
            return false;
        }

        // Ürün zaten satıldı, rezerve havuzundan kalıcı olarak düşülür
        if (stock.ReservedStock >= quantity)
        {
            stock.ReservedStock -= quantity;
        }
        else
        {
            stock.ReservedStock = 0;
        }

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stok rezervasyonu kesinleştirildi -> ProductId: {ProductId}, Miktar: {Quantity}", productId, quantity);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Stok onaylama çakışması! ProductId: {ProductId}", productId);
            _context.Entry(stock).State = EntityState.Detached;
            return false;
        }
    }

    // 🟢 Derleme hatasını önleyen güvenli alias:
    public Task<bool> IncreaseStockAsync(string productId, int quantity) => ReleaseStockAsync(productId, quantity);

    // 4. Yeni Ürün İçin Stok Oluşturma
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