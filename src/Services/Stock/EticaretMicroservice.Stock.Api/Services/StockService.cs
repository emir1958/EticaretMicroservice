using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Services;

public enum ReservationResult
{
    Reserved,
    InsufficientStock,
    AlreadyProcessed
}

public interface IStockService
{
    Task<ReservationResult> ReserveForOrderAsync(int orderId, IReadOnlyCollection<OrderItemMessage> items, CancellationToken cancellationToken = default);
    Task<int> ConfirmForOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<int> ReleaseForOrderAsync(int orderId, CancellationToken cancellationToken = default);
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

    public async Task<ReservationResult> ReserveForOrderAsync(int orderId, IReadOnlyCollection<OrderItemMessage> items, CancellationToken cancellationToken = default)
    {
        if (await _context.StockReservations.AnyAsync(r => r.OrderId == orderId, cancellationToken))
        {
            return ReservationResult.AlreadyProcessed;
        }

        var lines = items
            .GroupBy(i => i.ProductId)
            .Select(g => new StockLine(g.Key, g.Sum(i => i.Quantity)))
            .OrderBy(l => l.ProductId, StringComparer.Ordinal)
            .ToList();

        var reservedLines = new List<StockLine>();

        foreach (var line in lines)
        {
            var affectedRows = await _context.ProductStocks
                .Where(s => s.ProductId == line.ProductId && s.AvailableStock >= line.Quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.AvailableStock, x => x.AvailableStock - line.Quantity)
                    .SetProperty(x => x.ReservedStock, x => x.ReservedStock + line.Quantity),
                    cancellationToken);

            if (affectedRows == 0)
            {
                _logger.LogWarning("Yetersiz stok veya ürün bulunamadı. OrderId: {OrderId}, ProductId: {ProductId}, İstenen: {Quantity}",
                    orderId, line.ProductId, line.Quantity);

                foreach (var reserved in reservedLines)
                {
                    await AdjustStockAsync(reserved.ProductId, availableDelta: reserved.Quantity, reservedDelta: -reserved.Quantity, cancellationToken);
                }

                AddReservations(orderId, lines, StockReservationStatus.Rejected);
                return ReservationResult.InsufficientStock;
            }

            reservedLines.Add(line);
        }

        AddReservations(orderId, lines, StockReservationStatus.Reserved);

        _logger.LogInformation("OrderId: {OrderId} için {Count} kalem stok rezerve edildi.", orderId, lines.Count);
        return ReservationResult.Reserved;
    }

    // 2. Kesinleştirme - ödeme başarılı: rezerve havuzundan düşülür, müsait stoğa geri dönmez
    public async Task<int> ConfirmForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var reservations = await LockActiveReservationsAsync(orderId, cancellationToken);

        foreach (var reservation in reservations)
        {
            await AdjustStockAsync(reservation.ProductId, availableDelta: 0, reservedDelta: -reservation.Quantity, cancellationToken);
            reservation.Status = StockReservationStatus.Confirmed;
            reservation.UpdatedAt = DateTime.UtcNow;
        }

        return reservations.Count;
    }

    // 3. İade - ödeme başarısız veya zaman aşımı: yalnızca BU siparişin aktif rezervasyonu geri verilir
    public async Task<int> ReleaseForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var reservations = await LockActiveReservationsAsync(orderId, cancellationToken);

        foreach (var reservation in reservations)
        {
            await AdjustStockAsync(reservation.ProductId, availableDelta: reservation.Quantity, reservedDelta: -reservation.Quantity, cancellationToken);
            reservation.Status = StockReservationStatus.Released;
            reservation.UpdatedAt = DateTime.UtcNow;
        }

        return reservations.Count;
    }

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

    // UPDLOCK: aynı siparişe ait iki kopya mesaj (örn. iki PaymentFailedEvent) aynı anda gelirse
    // ikincisi burada bekler; ilki commit edince "Reserved" satır kalmadığını görür ve hiçbir şey yapmaz.
    private Task<List<StockReservation>> LockActiveReservationsAsync(int orderId, CancellationToken cancellationToken) =>
        _context.StockReservations
            .FromSqlInterpolated($"SELECT * FROM StockReservations WITH (UPDLOCK, ROWLOCK) WHERE OrderId = {orderId} AND Status = {(int)StockReservationStatus.Reserved}")
            .OrderBy(r => r.ProductId)
            .ToListAsync(cancellationToken);

    private Task<int> AdjustStockAsync(string productId, int availableDelta, int reservedDelta, CancellationToken cancellationToken) =>
        _context.ProductStocks
            .Where(s => s.ProductId == productId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.AvailableStock, x => x.AvailableStock + availableDelta)
                .SetProperty(x => x.ReservedStock, x => x.ReservedStock + reservedDelta),
                cancellationToken);

    private void AddReservations(int orderId, IEnumerable<StockLine> lines, StockReservationStatus status)
    {
        _context.StockReservations.AddRange(lines.Select(l => new StockReservation
        {
            OrderId = orderId,
            ProductId = l.ProductId,
            Quantity = l.Quantity,
            Status = status
        }));
    }

    private sealed record StockLine(string ProductId, int Quantity);
}
