using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Services;

public interface IStockService
{
    Task<bool> ReserveStockAsync(string productId, int quantity);
    Task<bool> ReleaseStockAsync(string productId, int quantity);
    Task<bool> ConfirmReservationAsync(string productId, int quantity);
    Task<bool> IncreaseStockAsync(string productId, int quantity);
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

    public async Task<bool> ReserveStockAsync(string productId, int quantity)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var stock = await _context.ProductStocks
                .FromSqlInterpolated($"SELECT * FROM ProductStocks WITH (UPDLOCK, ROWLOCK) WHERE ProductId = {productId}")
                .FirstOrDefaultAsync();

            if (stock == null)
            {
                _logger.LogWarning("Ürün stoğu bulunamadı: {ProductId}", productId);
                await transaction.RollbackAsync();
                return false;
            }

            if (stock.AvailableStock < quantity)
            {
                _logger.LogWarning("Yetersiz stok! Ürün: {ProductId}, Mevcut: {Available}, İstenen: {Quantity}",
                    productId, stock.AvailableStock, quantity);
                await transaction.RollbackAsync();
                return false;
            }

            stock.AvailableStock -= quantity;
            stock.ReservedStock += quantity;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Stok başarıyla rezerve edildi -> ProductId: {ProductId}, Rezerve: {Quantity}, Kalan: {Available}",
                productId, quantity, stock.AvailableStock);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Stok rezerve edilirken hata oluştu. ProductId: {ProductId}", productId);
            return false;
        }
    }

    // 2. Stok Rezervasyon İadesi (Release) - Ödeme iptalinde veya zaman aşımında
    public async Task<bool> ReleaseStockAsync(string productId, int quantity)
    { 
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var stock = await _context.ProductStocks
                .FromSqlInterpolated($"SELECT * FROM ProductStocks WITH (UPDLOCK, ROWLOCK) WHERE ProductId = {productId}")
                .FirstOrDefaultAsync();

            if (stock == null)
            {
                _logger.LogWarning("Stok iadesi yapılamadı. Ürün bulunamadı: {ProductId}", productId);
                await transaction.RollbackAsync();
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

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Stok rezervasyonu serbest bırakıldı -> ProductId: {ProductId}, İade: {Quantity}", productId, quantity);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Stok serbest bırakılırken hata! ProductId: {ProductId}", productId);
            return false;
        }
    }

    // 3. Rezervasyonu Onaylama (Commit) - Ödeme başarılı olduğunda rezerve havuzundan düşülür
    public async Task<bool> ConfirmReservationAsync(string productId, int quantity)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var stock = await _context.ProductStocks
                .FromSqlInterpolated($"SELECT * FROM ProductStocks WITH (UPDLOCK, ROWLOCK) WHERE ProductId = {productId}")
                .FirstOrDefaultAsync();

            if (stock == null)
            {
                _logger.LogWarning("Rezervasyon onaylanamadı. Ürün bulunamadı: {ProductId}", productId);
                await transaction.RollbackAsync();
                return false;
            }

            if (stock.ReservedStock >= quantity)
            {
                stock.ReservedStock -= quantity;
            }
            else
            {
                stock.ReservedStock = 0;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Stok rezervasyonu kesinleştirildi -> ProductId: {ProductId}, Kesinleşen: {Quantity}", productId, quantity);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Stok onaylanırken hata! ProductId: {ProductId}", productId);
            return false;
        }
    }

    public Task<bool> IncreaseStockAsync(string productId, int quantity) => ReleaseStockAsync(productId, quantity);

    // 4. Yeni Ürün Stok Kaydı
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