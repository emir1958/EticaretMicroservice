using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Application.Sagas;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Shared.Commands;
using FluentAssertions;

namespace EticaretMicroservice.IntegrationTests;

// OrderTimeoutSweeper: zamanlanmış timeout mesajı kaybolduğunda emniyet ağı. Order'ı kendisi değiştirmez,
// saga'ya PaymentTimeoutExpired gönderir. Taramalar testten doğrudan tetiklenir (hosted service aralığı beklenmez).
[Collection(OrderApiCollection.Name)]
public class OrderTimeoutSweeperTests
{
    private readonly OrderSagaTestDriver _driver;

    public OrderTimeoutSweeperTests(OrderApiFactory factory) => _driver = new OrderSagaTestDriver(factory);

    [Fact]
    public async Task OverduePendingOrder_IsCanceledThroughSaga()
    {
        var order = await _driver.CreateOrderAsync();
        await _driver.StockReservedAsync(order);
        await _driver.WaitForSagaStateAsync(order, "AwaitingPayment");
        await _driver.SetSagaExpiresAtAsync(order.CorrelationId, DateTime.UtcNow.AddHours(-1));

        var overdue = await _driver.Sweeper.FindOverdueAsync(DateTime.UtcNow, CancellationToken.None);
        overdue.Should().Contain(order.CorrelationId);

        await _driver.Sweeper.DispatchAsync(new[] { order.CorrelationId }, CancellationToken.None);

        await _driver.WaitForStatusAsync(order, OrderStatus.IptalEdildi);
        await OrderSagaTestDriver.WaitUntilAsync(
            () => Task.FromResult(_driver.ReceivedDownstreamCount<ReleaseStockCommand>(c => c.OrderId == order.OrderId) == 1),
            "ReleaseStockCommand gönderilmeli");
    }

    // Sweeper instance'ı beklemede okur, mesaj saga'ya ulaşmadan ödeme tamamlanır: geç gelen timeout hiçbir şeyi değiştirmemeli.
    [Fact]
    public async Task SweeperTimeout_ForOrderCompletedAfterSelection_ChangesNothing()
    {
        var order = await _driver.CreateOrderAsync();
        await _driver.StockReservedAsync(order);
        await _driver.WaitForSagaStateAsync(order, "AwaitingPayment");
        await _driver.SetSagaExpiresAtAsync(order.CorrelationId, DateTime.UtcNow.AddHours(-1));

        var overdue = await _driver.Sweeper.FindOverdueAsync(DateTime.UtcNow, CancellationToken.None);
        overdue.Should().Contain(order.CorrelationId);

        await _driver.PaymentCompletedAsync(order);
        await _driver.WaitForStatusAsync(order, OrderStatus.Tamamlandı);

        var timeoutConsumes = _driver.CountSagaConsumes<PaymentTimeoutExpired>(m => m.CorrelationId == order.CorrelationId);
        await _driver.Sweeper.DispatchAsync(new[] { order.CorrelationId }, CancellationToken.None);
        await OrderSagaTestDriver.WaitUntilAsync(() => Task.FromResult(timeoutConsumes.Count == 1), "sweeper timeout'u saga'da işlenmeli");

        (await _driver.GetStatusAsync(order.OrderId)).Should().Be(OrderStatus.Tamamlandı);
        (await _driver.GetSagaAsync(order.CorrelationId))!.CurrentState.Should().Be("Completed");
        _driver.ReceivedDownstreamCount<ReleaseStockCommand>(c => c.OrderId == order.OrderId).Should().Be(0);
    }

    // Saga instance'ı olmayan (saga öncesi) sipariş taramaya hiç girmez; fault üretecek mesaj gönderilmez
    [Fact]
    public async Task OrderWithoutSagaInstance_IsNotSelected()
    {
        var order = await _driver.CreateOrderWithoutSagaAsync();

        var overdue = await _driver.Sweeper.FindOverdueAsync(DateTime.UtcNow.AddDays(1), CancellationToken.None);

        overdue.Should().NotContain(order.CorrelationId);
    }
}
