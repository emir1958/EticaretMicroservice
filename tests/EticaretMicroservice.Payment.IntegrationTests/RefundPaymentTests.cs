using EticaretMicroservice.Payment.Api.Models;
using EticaretMicroservice.Payment.Api.Services;
using EticaretMicroservice.Payment.IntegrationTests.Infrastructure;
using EticaretMicroservice.Shared.Commands;
using EticaretMicroservice.Shared.Events;
using FluentAssertions;
using MassTransit;
using Moq;

namespace EticaretMicroservice.Payment.IntegrationTests;

[Collection(PaymentDatabaseCollection.Name)]
public class RefundPaymentTests
{
    private readonly PaymentDatabaseFixture _db;
    private readonly Mock<IPaymentService> _gateway = new();
    private int _refundCalls;

    public RefundPaymentTests(PaymentDatabaseFixture db)
    {
        _db = db;
        GatewayRefundSucceeds(delay: TimeSpan.Zero);
    }

    [Fact]
    public async Task Refund_ForSuccessfulPayment_RefundsAndMarksRefunded()
    {
        var payment = await _db.SeedPaymentAsync(PaymentStatus.Success, totalPrice: 250m);
        await using var host = await PaymentTestHost.StartAsync(_db.ConnectionString, _gateway.Object);

        await host.PublishAsync(Refund(payment));

        await PaymentTestHost.WaitUntilAsync(
            async () => await _db.GetStatusAsync(payment.OrderId) == PaymentStatus.Refunded, "ödeme Refunded olmalı");
        _gateway.Verify(g => g.RefundPayment(payment.OrderId, payment.PaymentToken, 250m), Times.Once);
    }

    // Order iadeyi bu event'ten öğrenir
    [Fact]
    public async Task Refund_ForSuccessfulPayment_PublishesRefundedEvent()
    {
        var payment = await _db.SeedPaymentAsync(PaymentStatus.Success, totalPrice: 250m);
        await using var host = await PaymentTestHost.StartAsync(_db.ConnectionString, _gateway.Object);

        await host.PublishAsync(Refund(payment));

        await PaymentTestHost.WaitUntilAsync(
            () => Task.FromResult(host.RefundedEventCount(payment.OrderId) == 1), "PaymentRefundedEvent yayınlanmalı");
        var refunded = host.Harness.Consumed.Select<PaymentRefundedEvent>().Single(m => m.Context.Message.OrderId == payment.OrderId);
        refunded.Context.Message.Amount.Should().Be(250m);
        refunded.Context.Message.CorrelationId.Should().Be(payment.CorrelationId);
    }

    // Kopya komut (örn. saga'ya iki kez gelen PaymentCompleted) ilk iade işlendikten sonra gelir
    [Fact]
    public async Task Refund_DeliveredTwice_RefundsOnlyOnce()
    {
        var payment = await _db.SeedPaymentAsync(PaymentStatus.Success);
        await using var host = await PaymentTestHost.StartAsync(_db.ConnectionString, _gateway.Object);

        await host.PublishAsync(Refund(payment));
        await PaymentTestHost.WaitUntilAsync(
            async () => await _db.GetStatusAsync(payment.OrderId) == PaymentStatus.Refunded, "ödeme Refunded olmalı");

        await host.PublishAsync(Refund(payment));
        await PaymentTestHost.SettleAsync();

        Volatile.Read(ref _refundCalls).Should().Be(1);
        host.RefundedEventCount(payment.OrderId).Should().Be(1);
    }

    // İki kopya aynı anda gelir. Sağlayıcı çağrısı yavaşlatılarak yarış penceresi genişletilir: kilit olmasa
    // ikisi de Success okuyup iki kez iade ederdi. UPDLOCK ile ikincisi bekler, Refunded görür ve hiçbir şey yapmaz.
    [Fact]
    public async Task Refund_ConcurrentDuplicates_RefundsOnlyOnce()
    {
        GatewayRefundSucceeds(delay: TimeSpan.FromMilliseconds(500));
        var payment = await _db.SeedPaymentAsync(PaymentStatus.Success);
        await using var host = await PaymentTestHost.StartAsync(_db.ConnectionString, _gateway.Object);

        await Task.WhenAll(host.PublishAsync(Refund(payment)), host.PublishAsync(Refund(payment)));

        await PaymentTestHost.WaitUntilAsync(
            async () => await _db.GetStatusAsync(payment.OrderId) == PaymentStatus.Refunded, "ödeme Refunded olmalı");
        await PaymentTestHost.SettleAsync();

        Volatile.Read(ref _refundCalls).Should().Be(1);
    }

    // Çekilmemiş para iade edilmez
    [Fact]
    public async Task Refund_ForFailedPayment_DoesNothing()
    {
        var payment = await _db.SeedPaymentAsync(PaymentStatus.Failed);
        await using var host = await PaymentTestHost.StartAsync(_db.ConnectionString, _gateway.Object);

        await host.PublishAsync(Refund(payment));
        (await host.Harness.Consumed.Any<RefundPaymentCommand>(m => m.Context.Message.OrderId == payment.OrderId)).Should().BeTrue();
        await PaymentTestHost.SettleAsync();

        (await _db.GetStatusAsync(payment.OrderId)).Should().Be(PaymentStatus.Failed);
        Volatile.Read(ref _refundCalls).Should().Be(0);
        host.RefundedEventCount(payment.OrderId).Should().Be(0);
    }

    // Sağlayıcı iadeyi reddederse durum değişmez ve mesaj hata verir (sessizce kaybolmaz)
    [Fact]
    public async Task Refund_WhenGatewayRejects_KeepsPaymentSuccessAndFaults()
    {
        _gateway.Setup(g => g.RefundPayment(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns((false, "Banka iadeyi reddetti"));
        var payment = await _db.SeedPaymentAsync(PaymentStatus.Success);
        await using var host = await PaymentTestHost.StartAsync(_db.ConnectionString, _gateway.Object);

        await host.PublishAsync(Refund(payment));

        (await host.Harness.Published.Any<Fault<RefundPaymentCommand>>(f => f.Context.Message.Message.OrderId == payment.OrderId))
            .Should().BeTrue();
        (await _db.GetStatusAsync(payment.OrderId)).Should().Be(PaymentStatus.Success);
        host.RefundedEventCount(payment.OrderId).Should().Be(0);
    }

    private void GatewayRefundSucceeds(TimeSpan delay) =>
        _gateway.Setup(g => g.RefundPayment(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Callback(() =>
            {
                Interlocked.Increment(ref _refundCalls);
                if (delay > TimeSpan.Zero)
                    Thread.Sleep(delay);
            })
            .Returns((true, string.Empty));

    private static RefundPaymentCommand Refund(PaymentRecord payment) => new()
    {
        CorrelationId = payment.CorrelationId,
        OrderId = payment.OrderId,
        Reason = "Sipariş iptal edildikten sonra ödeme alındı."
    };
}
