using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace EticaretMicroservice.Stock.IntegrationTests;

// Eşzamanlılık ve atomiklik kuralları. Bu testler refactor öncesi davranışı da koruma altına alır
// (characterization test): mevcut UPDLOCK yaklaşımı bunları zaten sağlıyor olmalı,
// refactor sonrası da sağlamaya devam etmeli.
[Collection(StockDatabaseCollection.Name)]
public class StockConcurrencyTests
{
    private readonly StockDatabaseFixture _db;

    public StockConcurrencyTests(StockDatabaseFixture db) => _db = db;

    // Son 5 ürüne aynı anda 20 sipariş gelirse tam olarak 5'i kazanmalı; stok asla eksiye düşmemeli (overselling).
    [Fact]
    public async Task ConcurrentOrders_ForLimitedStock_NeverOversell()
    {
        const int initialStock = 5;
        const int orderCount = 20;
        var productId = await _db.SeedProductAsync(initialStock);
        var orders = Enumerable.Range(0, orderCount)
            .Select(_ => StockIdempotencyTests.CreateOrderCreatedEvent(Random.Shared.Next(1_000_000, int.MaxValue), (productId, 1)))
            .ToList();
        var orderIds = orders.Select(o => o.OrderId).ToHashSet();
        await using var host = await StockTestHost.StartAsync(_db.ConnectionString);

        await Task.WhenAll(orders.Select(o => host.PublishAsync(o)));

        await StockTestHost.WaitUntilAsync(
            () => Task.FromResult(
                host.ReceivedDownstreamCount<StockReservedEvent>(e => orderIds.Contains(e.OrderId)) +
                host.ReceivedDownstreamCount<StockFailedEvent>(e => orderIds.Contains(e.OrderId)) == orderCount),
            "her sipariş için bir sonuç event'i yayınlanmalı");

        var stock = await _db.GetStockAsync(productId);
        stock.AvailableStock.Should().Be(0);
        stock.ReservedStock.Should().Be(initialStock);
        host.ReceivedDownstreamCount<StockReservedEvent>(e => orderIds.Contains(e.OrderId)).Should().Be(initialStock);
        host.ReceivedDownstreamCount<StockFailedEvent>(e => orderIds.Contains(e.OrderId)).Should().Be(orderCount - initialStock);
    }

    // Çok kalemli siparişte bir kalem yetersizse, diğer kalemler için yapılan rezervasyon geri alınmalı (hep ya da hiç).
    [Fact]
    public async Task OrderCreated_WhenOneItemIsOutOfStock_RollsBackPartialReservation()
    {
        var plentyProductId = await _db.SeedProductAsync(availableStock: 10);
        var scarceProductId = await _db.SeedProductAsync(availableStock: 1);
        var order = StockIdempotencyTests.CreateOrderCreatedEvent(
            Random.Shared.Next(1_000_000, int.MaxValue), (plentyProductId, 2), (scarceProductId, 5));
        await using var host = await StockTestHost.StartAsync(_db.ConnectionString);

        await host.PublishAsync(order);

        await StockTestHost.WaitUntilAsync(
            () => Task.FromResult(host.ReceivedDownstreamCount<StockFailedEvent>(e => e.OrderId == order.OrderId) == 1),
            "StockFailedEvent yayınlanmalı");

        var plenty = await _db.GetStockAsync(plentyProductId);
        plenty.AvailableStock.Should().Be(10);
        plenty.ReservedStock.Should().Be(0);
        var scarce = await _db.GetStockAsync(scarceProductId);
        scarce.AvailableStock.Should().Be(1);
        scarce.ReservedStock.Should().Be(0);
        host.ReceivedDownstreamCount<StockReservedEvent>(e => e.OrderId == order.OrderId).Should().Be(0);
    }
}
