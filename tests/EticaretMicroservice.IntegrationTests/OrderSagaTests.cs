using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Shared.Commands;
using EticaretMicroservice.Shared.Events;
using FluentAssertions;

namespace EticaretMicroservice.IntegrationTests;

// Order saga'sının sözleşmesi. Diğer servisler (Stock, Payment) event publish edilerek simüle edilir.
// Characterization testleri bugünkü choreography davranışını korur; saga'ya geçişte de yeşil kalmalıdır.
// Saga davranışı testleri saga uygulanana kadar kırmızıdır.
[Collection(OrderApiCollection.Name)]
public class OrderSagaTests
{
    private readonly OrderSagaTestDriver _driver;

    public OrderSagaTests(OrderApiFactory factory) => _driver = new OrderSagaTestDriver(factory);

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

    // --- Saga davranışı: saga uygulanana kadar kırmızı ---

    // Ödeme süre içinde gelmezse sipariş iptal edilir ve rezerve stok iade edilir.
    // Bugün: OrderTimeoutWorker 15 dakikaya sabit, OrderSaga:PaymentTimeout ayarını tanımıyor.
    [Fact]
    public async Task PaymentNotReceivedWithinTimeout_CancelsOrderAndReleasesStock()
    {
        var order = await _driver.CreateOrderAsync();
        await _driver.StockReservedAsync(order);

        await _driver.WaitForStatusAsync(order, OrderStatus.IptalEdildi);
        await OrderSagaTestDriver.WaitUntilAsync(
            () => Task.FromResult(_driver.ReceivedDownstreamCount<ReleaseStockCommand>(c => c.OrderId == order.OrderId) == 1),
            "ReleaseStockCommand gönderilmeli");
    }

    // Analizdeki 3. bulgu: timeout'tan sonra gelen ödeme müşteriden çekilmiş paradır; iade edilmelidir.
    // Bugün: iade adımı yok, para çekilmiş halde sipariş iptal kalır.
    [Fact]
    public async Task PaymentCompletedAfterTimeout_RefundsPaymentAndKeepsOrderCanceled()
    {
        var order = await _driver.CreateOrderAsync();
        await _driver.StockReservedAsync(order);
        await _driver.WaitForStatusAsync(order, OrderStatus.IptalEdildi);

        await _driver.PaymentCompletedAsync(order);

        await OrderSagaTestDriver.WaitUntilAsync(
            () => Task.FromResult(_driver.ReceivedDownstreamCount<RefundPaymentCommand>(c => c.OrderId == order.OrderId) == 1),
            "RefundPaymentCommand gönderilmeli");
        (await _driver.GetStatusAsync(order.OrderId)).Should().Be(OrderStatus.IptalEdildi);
    }

    // Sıra dışı (out-of-order) gelen StockFailed, tamamlanmış bir siparişi iptal etmemeli.
    // Bugün: StockFailedEventConsumer korumasız SetStatusToCanceled() kullanıyor.
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
