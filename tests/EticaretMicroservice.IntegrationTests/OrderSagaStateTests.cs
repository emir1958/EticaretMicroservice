using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Domain.Enums;
using FluentAssertions;

namespace EticaretMicroservice.IntegrationTests;

// Saga instance'ının kendisi: durumların gerçek SQL'de (EF saga repository) tutulması,
// eşzamanlı ve kopya event'lere karşı davranış.
[Collection(OrderApiCollection.Name)]
public class OrderSagaStateTests
{
    private const string AwaitingStock = "AwaitingStock";
    private const string AwaitingPayment = "AwaitingPayment";
    private const string Completed = "Completed";
    private const string Canceled = "Canceled";

    private readonly OrderSagaTestDriver _driver;

    public OrderSagaStateTests(OrderApiFactory factory) => _driver = new OrderSagaTestDriver(factory);

    // Harness varsayılan olarak saga repository'sini in-memory'ye çevirir; bu test kaydın gerçekten SQL'de olduğunu doğrular.
    [Fact]
    public async Task OrderCreated_CreatesSagaInstanceInSql_InAwaitingStock()
    {
        var order = await _driver.CreateOrderAsync();

        await _driver.WaitForSagaStateAsync(order, AwaitingStock);

        var saga = await _driver.GetSagaAsync(order.CorrelationId);
        saga!.OrderId.Should().Be(order.OrderId);
        saga.BuyerId.Should().Be(order.BuyerId);
    }

    [Fact]
    public async Task StockReserved_MovesSagaToAwaitingPayment()
    {
        var order = await _driver.CreateOrderAsync();

        await _driver.StockReservedAsync(order);

        await _driver.WaitForSagaStateAsync(order, AwaitingPayment);
        (await _driver.GetStatusAsync(order.OrderId)).Should().Be(OrderStatus.Beklemede);
    }

    // Kopya OrderCreated ikinci bir süreç başlatmamalı ve ilerlemiş süreci başa döndürmemeli.
    [Fact]
    public async Task DuplicateOrderCreated_DoesNotCreateSecondInstanceOrResetState()
    {
        var order = await _driver.CreateOrderAsync();
        await _driver.StockReservedAsync(order);
        await _driver.WaitForSagaStateAsync(order, AwaitingPayment);

        await _driver.OrderCreatedAsync(order);
        await OrderSagaTestDriver.SettleAsync();

        (await _driver.CountSagasForOrderAsync(order.OrderId)).Should().Be(1);
        (await _driver.GetSagaAsync(order.CorrelationId))!.CurrentState.Should().Be(AwaitingPayment);
    }

    // Çelişen iki event aynı anda gelirse tek bir sonuç kazanmalı; saga durumu ile sipariş durumu birbirini tutmalı.
    // Kaybeden mesaj çakışma alır, retry'da saga'yı terminal durumda bulur ve yoksayılır. Mevcut yapılandırmada
    // (outbox transaction'ı RepeatableRead) çakışma SQL deadlock'u olarak çıkar; RowVersion ikinci savunma hattıdır.
    [Fact]
    public async Task PaymentCompletedAndFailedArriveConcurrently_SagaAndOrderStayConsistent()
    {
        var order = await _driver.CreateOrderAsync();
        await _driver.StockReservedAsync(order);
        await _driver.WaitForSagaStateAsync(order, AwaitingPayment);

        await Task.WhenAll(_driver.PaymentCompletedAsync(order), _driver.PaymentFailedAsync(order));

        await OrderSagaTestDriver.WaitUntilAsync(
            async () => (await _driver.GetSagaAsync(order.CorrelationId))?.CurrentState is Completed or Canceled,
            "saga terminal duruma ulaşmalı");
        await OrderSagaTestDriver.SettleAsync();

        var sagaState = (await _driver.GetSagaAsync(order.CorrelationId))!.CurrentState;
        var expectedOrderStatus = sagaState == Completed ? OrderStatus.Tamamlandı : OrderStatus.IptalEdildi;
        (await _driver.GetStatusAsync(order.OrderId)).Should().Be(expectedOrderStatus);
    }
}
