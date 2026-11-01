using EticaretMicroservice.Shared.Commands;
using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.IntegrationTests.Infrastructure;
using FluentAssertions;
using static EticaretMicroservice.Stock.IntegrationTests.StockIdempotencyTests;

namespace EticaretMicroservice.Stock.IntegrationTests;

// ReleaseStockCommand: Order saga'sı siparişi iptal etti (ödeme süresi doldu). Sipariş iptal olduğu için o siparişin
// rezerve ve kesinleşmiş stoğu müsait stoğa döner; kopya komut ve diğer siparişler etkilenmez.
[Collection(StockDatabaseCollection.Name)]
public class StockReleaseForCanceledOrderTests
{
    private readonly StockDatabaseFixture _db;

    public StockReleaseForCanceledOrderTests(StockDatabaseFixture db) => _db = db;

    [Fact]
    public async Task ReleaseStock_ForReservedOrder_DeliveredTwice_ReleasesOnlyThatOrder()
    {
        var productId = await _db.SeedProductAsync(availableStock: 10);
        var orderA = CreateOrderCreatedEvent(NewOrderId(), (productId, 2));
        var orderB = CreateOrderCreatedEvent(NewOrderId(), (productId, 3));
        await using var host = await StockTestHost.StartAsync(_db.ConnectionString);

        await host.PublishAsync(orderA);
        await host.PublishAsync(orderB);
        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).ReservedStock == 5, "iki sipariş de rezerve edilmeli");

        var releaseA = ReleaseStock(orderA);
        await host.PublishAsync(releaseA);
        await host.PublishAsync(releaseA);

        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).AvailableStock > 5, "A'nın stoğu iade edilmeli");
        await StockTestHost.SettleAsync();

        var stock = await _db.GetStockAsync(productId);
        stock.AvailableStock.Should().Be(7, "yalnızca A'nın 2 adedi bir kez iade edilmeli");
        stock.ReservedStock.Should().Be(3, "B'nin rezervasyonu korunmalı");
    }

    // Timeout/ödeme yarışı: Stock, saga'nın ReleaseStockCommand'ından önce PaymentCompleted'ı işleyip rezervasyonu
    // kesinleştirmiş. Saga siparişi iptal edip ödemeyi iade ettiği için satılmış sayılan stok geri dönmeli.
    [Fact]
    public async Task ReleaseStock_AfterPaymentConfirmed_DeliveredTwice_ReturnsSoldStockOnce()
    {
        var productId = await _db.SeedProductAsync(availableStock: 10);
        var orderA = CreateOrderCreatedEvent(NewOrderId(), (productId, 2));
        var orderB = CreateOrderCreatedEvent(NewOrderId(), (productId, 3));
        await using var host = await StockTestHost.StartAsync(_db.ConnectionString);

        await host.PublishAsync(orderA);
        await host.PublishAsync(orderB);
        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).ReservedStock == 5, "iki sipariş de rezerve edilmeli");

        await host.PublishAsync(new PaymentCompletedEvent
        {
            CorrelationId = orderA.CorrelationId,
            OrderId = orderA.OrderId,
            BuyerId = orderA.BuyerId,
            OrderItems = orderA.OrderItems
        });
        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).ReservedStock == 3, "A kesinleşmeli");

        var releaseA = ReleaseStock(orderA);
        await host.PublishAsync(releaseA);
        await host.PublishAsync(releaseA);

        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).AvailableStock > 5, "A'nın kesinleşmiş stoğu iade edilmeli");
        await StockTestHost.SettleAsync();

        var stock = await _db.GetStockAsync(productId);
        stock.AvailableStock.Should().Be(7, "A'nın 2 adedi bir kez müsait stoğa dönmeli");
        stock.ReservedStock.Should().Be(3, "B'nin rezervasyonu korunmalı, A kesinleşirken rezerveden zaten düşülmüştü");
    }

    private static ReleaseStockCommand ReleaseStock(OrderCreatedEvent order) => new()
    {
        CorrelationId = order.CorrelationId,
        OrderId = order.OrderId,
        Reason = "Ödeme süresi doldu."
    };

    private static int NewOrderId() => Random.Shared.Next(1_000_000, int.MaxValue);
}
