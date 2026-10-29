using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace EticaretMicroservice.Stock.IntegrationTests;

// "Aynı mesaj iki kez gelirse ne olur?"
// RabbitMQ at-least-once teslim eder: consumer çökmesi, ack kaybı, outbox'ın yeniden göndermesi gibi
// durumlarda aynı event ikinci kez gelebilir. Stok hareketleri bu durumda tekrar uygulanmamalıdır.
[Collection(StockDatabaseCollection.Name)]
public class StockIdempotencyTests
{
    private readonly StockDatabaseFixture _db;

    public StockIdempotencyTests(StockDatabaseFixture db) => _db = db;

    // Teknik savunma hattı: aynı MessageId ile gelen kopya Inbox tarafından elenmeli.
    [Fact]
    public async Task OrderCreated_DeliveredTwiceWithSameMessageId_ReservesStockOnlyOnce()
    {
        var productId = await _db.SeedProductAsync(availableStock: 10);
        var orderCreated = CreateOrderCreatedEvent(NewOrderId(), (productId, 3));
        var messageId = Guid.NewGuid();
        await using var host = await StockTestHost.StartAsync(_db.ConnectionString);

        // Kopya, ilk mesaj işlendikten sonra gönderilir (gerçek hayatta: ack kaybı sonrası redelivery).
        // İkisini aynı anda göndermek yarış durumu yaratır ve testi yanlış sebeple yeşil yapabilir.
        await host.PublishAsync(orderCreated, messageId);
        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).ReservedStock > 0, "stok rezerve edilmeli");

        await host.PublishAsync(orderCreated, messageId);
        await StockTestHost.SettleAsync();

        var stock = await _db.GetStockAsync(productId);
        stock.AvailableStock.Should().Be(7);
        stock.ReservedStock.Should().Be(3);
        host.ReceivedDownstreamCount<StockReservedEvent>(e => e.OrderId == orderCreated.OrderId).Should().Be(1);
    }

    // İş kuralı savunma hattı: kopya farklı MessageId ile gelse bile (örn. Order servisi aynı siparişi
    // yeniden publish ederse) aynı sipariş için ikinci rezervasyon yapılmamalı.
    [Fact]
    public async Task OrderCreated_RedeliveredWithNewMessageId_ReservesStockOnlyOnce()
    {
        var productId = await _db.SeedProductAsync(availableStock: 10);
        var orderCreated = CreateOrderCreatedEvent(NewOrderId(), (productId, 3));
        await using var host = await StockTestHost.StartAsync(_db.ConnectionString);

        await host.PublishAsync(orderCreated);
        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).ReservedStock > 0, "stok rezerve edilmeli");

        await host.PublishAsync(orderCreated);
        await StockTestHost.SettleAsync();

        var stock = await _db.GetStockAsync(productId);
        stock.AvailableStock.Should().Be(7);
        stock.ReservedStock.Should().Be(3);
        host.ReceivedDownstreamCount<StockReservedEvent>(e => e.OrderId == orderCreated.OrderId).Should().Be(1);
    }

    // Yetersiz stok kararı da kalıcı olmalı: kopya mesaj, iptal edilmiş bir sipariş için
    // sonradan stok rezerve edip ödeme akışını başlatmamalı.
    [Fact]
    public async Task OrderCreated_RejectedForInsufficientStock_DuplicateDoesNotReserveLater()
    {
        var productId = await _db.SeedProductAsync(availableStock: 1);
        var orderCreated = CreateOrderCreatedEvent(NewOrderId(), (productId, 5));
        await using var host = await StockTestHost.StartAsync(_db.ConnectionString);

        await host.PublishAsync(orderCreated);
        await StockTestHost.WaitUntilAsync(
            () => Task.FromResult(host.ReceivedDownstreamCount<StockFailedEvent>(e => e.OrderId == orderCreated.OrderId) == 1),
            "StockFailedEvent yayınlanmalı");

        // Bu arada stok yenilenir; kopya mesaj artık "yeterli stok" görebilir
        await using (var context = _db.CreateDbContext())
        {
            var stock = context.ProductStocks.Single(s => s.ProductId == productId);
            stock.AvailableStock = 50;
            await context.SaveChangesAsync();
        }

        await host.PublishAsync(orderCreated);
        await StockTestHost.SettleAsync();

        var after = await _db.GetStockAsync(productId);
        after.AvailableStock.Should().Be(50);
        after.ReservedStock.Should().Be(0);
        host.ReceivedDownstreamCount<StockReservedEvent>(e => e.OrderId == orderCreated.OrderId).Should().Be(0);
    }

    // Telafi (compensation) adımı da idempotent olmalı ve yalnızca ilgili siparişin rezervasyonuna dokunmalı.
    [Fact]
    public async Task PaymentFailed_DeliveredTwice_ReleasesOnlyThatOrdersReservation()
    {
        var productId = await _db.SeedProductAsync(availableStock: 10);
        var orderA = CreateOrderCreatedEvent(NewOrderId(), (productId, 2));
        var orderB = CreateOrderCreatedEvent(NewOrderId(), (productId, 3));
        await using var host = await StockTestHost.StartAsync(_db.ConnectionString);

        await host.PublishAsync(orderA);
        await host.PublishAsync(orderB);
        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).ReservedStock == 5, "iki sipariş de rezerve edilmeli");

        var paymentFailedA = new PaymentFailedEvent
        {
            CorrelationId = orderA.CorrelationId,
            OrderId = orderA.OrderId,
            BuyerId = orderA.BuyerId,
            Message = "Kart reddedildi",
            OrderItems = orderA.OrderItems
        };
        await host.PublishAsync(paymentFailedA);
        await host.PublishAsync(paymentFailedA);

        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).AvailableStock > 5, "A'nın stoğu iade edilmeli");
        await StockTestHost.SettleAsync();

        var stock = await _db.GetStockAsync(productId);
        stock.AvailableStock.Should().Be(7, "yalnızca A'nın 2 adedi iade edilmeli");
        stock.ReservedStock.Should().Be(3, "B'nin rezervasyonu korunmalı");
    }

    // Kesinleştirme adımı iki kez gelirse başka siparişlerin rezervasyonu tüketilmemeli.
    [Fact]
    public async Task PaymentCompleted_DeliveredTwice_ConfirmsOnlyThatOrdersReservation()
    {
        var productId = await _db.SeedProductAsync(availableStock: 10);
        var orderA = CreateOrderCreatedEvent(NewOrderId(), (productId, 2));
        var orderB = CreateOrderCreatedEvent(NewOrderId(), (productId, 3));
        await using var host = await StockTestHost.StartAsync(_db.ConnectionString);

        await host.PublishAsync(orderA);
        await host.PublishAsync(orderB);
        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).ReservedStock == 5, "iki sipariş de rezerve edilmeli");

        var paymentCompletedA = new PaymentCompletedEvent
        {
            CorrelationId = orderA.CorrelationId,
            OrderId = orderA.OrderId,
            BuyerId = orderA.BuyerId,
            OrderItems = orderA.OrderItems
        };
        await host.PublishAsync(paymentCompletedA);
        await host.PublishAsync(paymentCompletedA);

        await StockTestHost.WaitUntilAsync(
            async () => (await _db.GetStockAsync(productId)).ReservedStock < 5, "A kesinleşmeli");
        await StockTestHost.SettleAsync();

        var stock = await _db.GetStockAsync(productId);
        stock.AvailableStock.Should().Be(5, "satılan stok müsait stoğa geri dönmez");
        stock.ReservedStock.Should().Be(3, "B'nin rezervasyonu korunmalı");
    }

    private static int NewOrderId() => Random.Shared.Next(1_000_000, int.MaxValue);

    internal static OrderCreatedEvent CreateOrderCreatedEvent(int orderId, params (string ProductId, int Quantity)[] items) => new()
    {
        CorrelationId = Guid.NewGuid(),
        OrderId = orderId,
        BuyerId = "it-buyer",
        PaymentToken = "tok_it_success",
        OrderItems = items.Select(i => new OrderItemMessage
        {
            ProductId = i.ProductId,
            ProductName = "Integration Test Product",
            Quantity = i.Quantity,
            Price = 10m
        }).ToList()
    };
}
