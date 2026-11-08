using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Application.Sagas;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Shared.Commands;
using EticaretMicroservice.Shared.Events;
using FluentAssertions;

namespace EticaretMicroservice.IntegrationTests;

// Order saga'sının sözleşmesi. Diğer servisler (Stock, Payment) event publish edilerek simüle edilir.
// Characterization testleri bugünkü choreography davranışını korur; saga'ya geçişte de yeşil kalmalıdır.
// Saga davranışı testleri saga'nın durum geçişlerini, timeout'u ve telafi (compensation) adımlarını doğrular.
[Collection(OrderApiCollection.Name)]
public class OrderSagaTests
{
    private readonly OrderApiFactory _factory;
    private readonly OrderSagaTestDriver _driver;
    private OrderSagaTestDriver? _shortTimeoutDriver;

    public OrderSagaTests(OrderApiFactory factory)
    {
        _factory = factory;
        _driver = new OrderSagaTestDriver(factory);
    }

    // Timeout senaryoları global (uzun) PaymentTimeout'a güvenmez; kısa timeout'lu host ilk kullanımda başlatılır
    private OrderSagaTestDriver TimeoutDriver => _shortTimeoutDriver ??= OrderSagaTestDriver.WithShortTimeout(_factory);

    // --- Characterization: bugünkü davranış ---

    [Fact]
    public async Task StockReservedThenPaymentCompleted_CompletesOrder()
    {
        var order = await _driver.CreateOrderAsync();

        await _driver.StockReservedAsync(order);
        await _driver.PaymentCompletedAsync(order);

        await _driver.WaitForStatusAsync(order, OrderStatus.Tamamlandı);
    }

    [Fact]
    public async Task StockFailed_CancelsOrder()
    {
        var order = await _driver.CreateOrderAsync();

        await _driver.StockFailedAsync(order);

        await _driver.WaitForStatusAsync(order, OrderStatus.IptalEdildi);
    }

    [Fact]
    public async Task PaymentFailed_CancelsOrder()
    {
        var order = await _driver.CreateOrderAsync();

        await _driver.StockReservedAsync(order);
        await _driver.PaymentFailedAsync(order);

        await _driver.WaitForStatusAsync(order, OrderStatus.IptalEdildi);
    }

    // Kopya PaymentCompleted "geç gelen ödeme" ile karıştırılmamalı: tamamlanmış siparişte refund istenmez.
    [Fact]
    public async Task DuplicatePaymentCompleted_OnCompletedOrder_DoesNotRequestRefund()
    {
        var order = await _driver.CreateOrderAsync();
        await _driver.StockReservedAsync(order);
        await _driver.PaymentCompletedAsync(order);
        await _driver.WaitForStatusAsync(order, OrderStatus.Tamamlandı);

        await _driver.PaymentCompletedAsync(order);
        await OrderSagaTestDriver.SettleAsync();

        (await _driver.GetStatusAsync(order.OrderId)).Should().Be(OrderStatus.Tamamlandı);
        _driver.ReceivedDownstreamCount<RefundPaymentCommand>(c => c.OrderId == order.OrderId).Should().Be(0);
    }

    // --- Saga davranışı ---

    // Ödeme süre içinde gelmezse sipariş iptal edilir ve rezerve stok iade edilir (saga Schedule, OrderSaga:PaymentTimeout).
    [Fact]
    public async Task PaymentNotReceivedWithinTimeout_CancelsOrderAndReleasesStock()
    {
        var order = await TimeoutDriver.CreateOrderAsync();
        await TimeoutDriver.StockReservedAsync(order);

        await TimeoutDriver.WaitForStatusAsync(order, OrderStatus.IptalEdildi);
        await OrderSagaTestDriver.WaitUntilAsync(
            () => Task.FromResult(TimeoutDriver.ReceivedDownstreamCount<ReleaseStockCommand>(c => c.OrderId == order.OrderId) == 1),
            "ReleaseStockCommand gönderilmeli");
    }

    // ExpiresAt emniyet kontrolü: süre dolmadan gelen timeout mesajı (erken teslim / saat farkı) siparişi iptal etmez,
    // saga kalan süre için yeniden zamanlar ve sipariş süre dolunca bir kez iptal edilir.
    [Fact]
    public async Task TimeoutArrivingBeforeExpiresAt_DoesNotCancelUntilExpired()
    {
        var order = await TimeoutDriver.CreateOrderAsync();
        await TimeoutDriver.StockReservedAsync(order);
        await TimeoutDriver.WaitForSagaStateAsync(order, "AwaitingPayment");
        var timeoutConsumes = TimeoutDriver.CountSagaConsumes<PaymentTimeoutExpired>(m => m.CorrelationId == order.CorrelationId);

        await TimeoutDriver.PaymentTimeoutAsync(order);
        await OrderSagaTestDriver.WaitUntilAsync(() => Task.FromResult(timeoutConsumes.Count == 1), "erken timeout işlenmeli");

        (await TimeoutDriver.GetStatusAsync(order.OrderId)).Should().Be(OrderStatus.Beklemede);

        await TimeoutDriver.WaitForStatusAsync(order, OrderStatus.IptalEdildi);
        await OrderSagaTestDriver.SettleAsync();
        TimeoutDriver.ReceivedDownstreamCount<ReleaseStockCommand>(c => c.OrderId == order.OrderId).Should().Be(1);
    }

    // Tamamlanmış siparişe geç gelen (zamanlanmış ya da başıboş) timeout mesajı iptal etmemeli, stok iadesi istememeli.
    [Fact]
    public async Task TimeoutAfterCompletion_DoesNotCancelCompletedOrder()
    {
        var order = await TimeoutDriver.CreateOrderAsync();
        await TimeoutDriver.StockReservedAsync(order);
        await TimeoutDriver.PaymentCompletedAsync(order);
        await TimeoutDriver.WaitForStatusAsync(order, OrderStatus.Tamamlandı);

        await TimeoutDriver.PaymentTimeoutAsync(order);
        await Task.Delay(OrderApiFactory.ShortPaymentTimeout);
        await OrderSagaTestDriver.SettleAsync();

        (await TimeoutDriver.GetStatusAsync(order.OrderId)).Should().Be(OrderStatus.Tamamlandı);
        (await TimeoutDriver.GetSagaAsync(order.CorrelationId))!.CurrentState.Should().Be("Completed");
        TimeoutDriver.ReceivedDownstreamCount<ReleaseStockCommand>(c => c.OrderId == order.OrderId).Should().Be(0);
    }

    // Timeout stok cevabından önce gelirse Stock rezervasyonu iptalden sonra yapmış olabilir:
    // Canceled durumunda gelen StockReserved için stok iadesi yeniden istenir, sipariş iptal kalır.
    [Fact]
    public async Task StockReservedAfterTimeoutCancel_RequestsStockReleaseAgain()
    {
        var order = await TimeoutDriver.CreateOrderAsync();
        await TimeoutDriver.WaitForStatusAsync(order, OrderStatus.IptalEdildi);
        await OrderSagaTestDriver.WaitUntilAsync(
            () => Task.FromResult(TimeoutDriver.ReceivedDownstreamCount<ReleaseStockCommand>(c => c.OrderId == order.OrderId) == 1),
            "timeout ReleaseStockCommand göndermeli");

        await TimeoutDriver.StockReservedAsync(order);

        await OrderSagaTestDriver.WaitUntilAsync(
            () => Task.FromResult(TimeoutDriver.ReceivedDownstreamCount<ReleaseStockCommand>(c => c.OrderId == order.OrderId) == 2),
            "geç StockReserved için ReleaseStockCommand yeniden gönderilmeli");
        (await TimeoutDriver.GetStatusAsync(order.OrderId)).Should().Be(OrderStatus.IptalEdildi);
        (await TimeoutDriver.GetSagaAsync(order.CorrelationId))!.CurrentState.Should().Be("Canceled");
    }

    // Analizdeki 3. bulgu: timeout'tan sonra gelen ödeme müşteriden çekilmiş paradır; iade edilmelidir.
    [Fact]
    public async Task PaymentCompletedAfterTimeout_RefundsPaymentAndKeepsOrderCanceled()
    {
        var order = await TimeoutDriver.CreateOrderAsync();
        await TimeoutDriver.StockReservedAsync(order);
        await TimeoutDriver.WaitForStatusAsync(order, OrderStatus.IptalEdildi);

        await TimeoutDriver.PaymentCompletedAsync(order);

        await OrderSagaTestDriver.WaitUntilAsync(
            () => Task.FromResult(TimeoutDriver.ReceivedDownstreamCount<RefundPaymentCommand>(c => c.OrderId == order.OrderId) == 1),
            "RefundPaymentCommand gönderilmeli");
        (await TimeoutDriver.GetStatusAsync(order.OrderId)).Should().Be(OrderStatus.IptalEdildi);
    }

    // Sıra dışı (out-of-order) gelen StockFailed, tamamlanmış bir siparişi iptal etmemeli.
    [Fact]
    public async Task StockFailedAfterCompletion_DoesNotCancelCompletedOrder()
    {
        var order = await _driver.CreateOrderAsync();
        await _driver.StockReservedAsync(order);
        await _driver.PaymentCompletedAsync(order);
        await _driver.WaitForStatusAsync(order, OrderStatus.Tamamlandı);

        await _driver.StockFailedAsync(order);
        await OrderSagaTestDriver.SettleAsync();

        (await _driver.GetStatusAsync(order.OrderId)).Should().Be(OrderStatus.Tamamlandı);
    }
}
