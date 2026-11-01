using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Shared.Events;
using FluentAssertions;
using MassTransit;

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

    // Henüz instance yokken aynı anda gelen iki OrderCreated: kilitlenecek satır olmadığından ikisi de INSERT dener.
    // Kaybeden PK ihlali (2627) alır, retry'da instance'ı bulur ve Ignore(OrderCreated) ile yoksayılır; mesaj fault'a düşmez.
    // HTTP akışı kendi OrderCreated'ını da ürettiği için sipariş OrderCreated yayınlanmadan yazılır, iki kopyayı test gönderir.
    [Fact]
    public async Task DuplicateOrderCreatedArriveConcurrently_CreatesSingleInstanceWithoutFault()
    {
        var order = await _driver.CreateOrderWithoutSagaAsync();
        var sagaConsumes = _driver.CountSagaConsumes<OrderCreatedEvent>(m => m.CorrelationId == order.CorrelationId);

        await Task.WhenAll(_driver.OrderCreatedAsync(order), _driver.OrderCreatedAsync(order));

        await _driver.WaitForSagaStateAsync(order, AwaitingStock);
        await OrderSagaTestDriver.WaitUntilAsync(() => Task.FromResult(sagaConsumes.Count == 2),
            "iki OrderCreated da saga tarafından işlenmeli");

        (await _driver.CountSagasForOrderAsync(order.OrderId)).Should().Be(1);
        _driver.Harness.Published.Select<Fault<OrderCreatedEvent>>()
            .Count(f => f.Context.Message.Message.CorrelationId == order.CorrelationId).Should().Be(0);
    }

    // Çelişen iki event aynı anda gelirse tek bir sonuç kazanmalı; saga durumu ile sipariş durumu birbirini tutmalı.
    // Pessimistic saga repository'de ikinci event instance satırının kilidinde (UPDLOCK) bekler, ilki commit edince
    // saga'yı terminal durumda bulur ve yoksayılır; deadlock/retry gerekmez.
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
