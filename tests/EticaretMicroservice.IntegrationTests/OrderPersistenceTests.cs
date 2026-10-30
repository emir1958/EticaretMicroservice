using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Application.Sagas;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EticaretMicroservice.IntegrationTests;

// Saga'nın dayandığı kalıcılık kuralları: siparişin CorrelationId'si ve OrderState tablosu (gerçek SQL Server).
[Collection(OrderApiCollection.Name)]
public class OrderPersistenceTests
{
    private readonly OrderApiFactory _factory;
    private readonly OrderSagaTestDriver _driver;

    public OrderPersistenceTests(OrderApiFactory factory)
    {
        _factory = factory;
        _driver = new OrderSagaTestDriver(factory);
    }

    // Her sipariş kendi CorrelationId'sini sunucuda alır; istemcinin header'ı saga anahtarını belirleyemez.
    [Fact]
    public async Task CreateOrder_AssignsUniqueServerGeneratedCorrelationId()
    {
        var first = await _driver.CreateOrderAsync();
        var second = await _driver.CreateOrderAsync();

        first.CorrelationId.Should().NotBe(Guid.Empty);
        second.CorrelationId.Should().NotBe(Guid.Empty);
        first.CorrelationId.Should().NotBe(second.CorrelationId);
    }

    [Fact]
    public async Task OrderState_RoundTripsAllFields()
    {
        var state = NewOrderState();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            dbContext.Set<OrderState>().Add(state);
            await dbContext.SaveChangesAsync();
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var loaded = await dbContext.Set<OrderState>().AsNoTracking().SingleAsync(s => s.CorrelationId == state.CorrelationId);

            loaded.CurrentState.Should().Be(state.CurrentState);
            loaded.OrderId.Should().Be(state.OrderId);
            loaded.BuyerId.Should().Be(state.BuyerId);
            loaded.CreatedAt.Should().BeCloseTo(state.CreatedAt, TimeSpan.FromMilliseconds(1));
            loaded.ExpiresAt.Should().BeCloseTo(state.ExpiresAt!.Value, TimeSpan.FromMilliseconds(1));
            loaded.PaymentTimeoutTokenId.Should().Be(state.PaymentTimeoutTokenId);
            loaded.RowVersion.Should().NotBeNullOrEmpty();
        }
    }

    // Aynı siparişe ait iki event aynı anda işlenirse (örn. PaymentCompleted ile timeout) ikinci yazım
    // sessizce birincinin üzerine yazmamalı; RowVersion çakışmayı yakalamalı ve mesaj retry ile yeniden işlenmeli.
    [Fact]
    public async Task OrderState_ConcurrentUpdates_SecondWriterGetsConcurrencyConflict()
    {
        var state = NewOrderState();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            dbContext.Set<OrderState>().Add(state);
            await dbContext.SaveChangesAsync();
        }

        await using var scopeA = _factory.Services.CreateAsyncScope();
        await using var scopeB = _factory.Services.CreateAsyncScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<OrderDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<OrderDbContext>();

        var instanceA = await contextA.Set<OrderState>().SingleAsync(s => s.CorrelationId == state.CorrelationId);
        var instanceB = await contextB.Set<OrderState>().SingleAsync(s => s.CorrelationId == state.CorrelationId);

        instanceA.CurrentState = "Completed";
        await contextA.SaveChangesAsync();

        instanceB.CurrentState = "Canceled";
        var act = () => contextB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // Bir siparişin yalnızca bir saga instance'ı olabilir.
    [Fact]
    public async Task OrderState_SameOrderIdTwice_IsRejected()
    {
        var first = NewOrderState();
        var duplicate = NewOrderState();
        duplicate.OrderId = first.OrderId;

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        dbContext.Set<OrderState>().Add(first);
        await dbContext.SaveChangesAsync();

        dbContext.Set<OrderState>().Add(duplicate);
        var act = () => dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private static OrderState NewOrderState() => new()
    {
        CorrelationId = NewId.NextGuid(),
        CurrentState = "AwaitingPayment",
        OrderId = Random.Shared.Next(1_000_000, int.MaxValue),
        BuyerId = "persistence-buyer",
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        PaymentTimeoutTokenId = Guid.NewGuid()
    };
}
