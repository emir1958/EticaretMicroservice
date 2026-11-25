using EticaretMicroservice.Services.Order.Domain.Entities;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Services.Order.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace EticaretMicroservice.UnitTests.Order;

public class OrderDomainTests
{
    private static Address CreateSampleAddress() =>
        new("Test Alıcı", "", "İstanbul", "Kadıköy", "Moda Cad.", "34710", "No:1 Daire:5");

    private static Services.Order.Domain.Entities.Order CreateSampleOrder() =>
        new("buyer-123", CreateSampleAddress(), Guid.NewGuid());

    [Fact]
    public void Order_WhenCreated_ShouldKeepGivenCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid();

        // Act
        var order = new Services.Order.Domain.Entities.Order("buyer-123", CreateSampleAddress(), correlationId);

        // Assert
        order.CorrelationId.Should().Be(correlationId);
    }

    // Saga anahtarı olduğu için boş CorrelationId ile sipariş oluşturulamaz
    [Fact]
    public void Order_WhenCorrelationIdIsEmpty_ShouldThrow()
    {
        // Act
        var act = () => new Services.Order.Domain.Entities.Order("buyer-123", CreateSampleAddress(), Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Order_WhenCreated_ShouldHaveBeklemedeStatus()
    {
        // Arrange & Act
        var order = CreateSampleOrder();

        // Assert
        order.OrderStatus.Should().Be(OrderStatus.Beklemede);
    }

    [Fact]
    public void TrySetStatusToCompleted_WhenStatusIsBeklemede_ShouldReturnTrueAndSetTamamlandi()
    {
        // Arrange
        var order = CreateSampleOrder();

        // Act
        var result = order.TrySetStatusToCompleted(DateTime.UtcNow);

        // Assert
        result.Should().BeTrue();
        order.OrderStatus.Should().Be(OrderStatus.Tamamlandı);
    }

    [Fact]
    public void TrySetStatusToCompleted_WhenOrderIsAlreadyCanceled_ShouldReturnFalseAndKeepIptalEdildi()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.TrySetStatusToCanceled(DateTime.UtcNow);

        // Act
        var result = order.TrySetStatusToCompleted(DateTime.UtcNow);

        // Assert
        result.Should().BeFalse();
        order.OrderStatus.Should().Be(OrderStatus.IptalEdildi);
    }

    [Fact]
    public void TrySetStatusToCanceled_WhenOrderIsAlreadyCompleted_ShouldReturnFalseAndKeepTamamlandi()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.TrySetStatusToCompleted(DateTime.UtcNow);

        // Act
        var result = order.TrySetStatusToCanceled(DateTime.UtcNow);

        // Assert
        result.Should().BeFalse();
        order.OrderStatus.Should().Be(OrderStatus.Tamamlandı);
    }

    [Fact]
    public void Order_WhenCreated_ShouldHaveAlinmadiPaymentStatus()
    {
        var order = CreateSampleOrder();

        order.PaymentStatus.Should().Be(PaymentStatus.Alinmadi);
        order.RefundedAt.Should().BeNull();
    }

    [Fact]
    public void TryMarkRefunded_WhenPaymentReceived_ShouldSetRefundedAt()
    {
        var order = CreateSampleOrder();
        order.TryMarkPaymentReceived(DateTime.UtcNow);
        var refundedAt = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);

        var result = order.TryMarkRefunded(refundedAt);

        result.Should().BeTrue();
        order.PaymentStatus.Should().Be(PaymentStatus.IadeEdildi);
        order.RefundedAt.Should().Be(refundedAt);
    }

    // Alınmamış ödeme iade edilmiş sayılmaz
    [Fact]
    public void TryMarkRefunded_WhenPaymentNotReceived_ShouldReturnFalse()
    {
        var order = CreateSampleOrder();

        var result = order.TryMarkRefunded(DateTime.UtcNow);

        result.Should().BeFalse();
        order.PaymentStatus.Should().Be(PaymentStatus.Alinmadi);
        order.RefundedAt.Should().BeNull();
    }

    // Kopya iade bildirimi ilk iade tarihini ezmemeli
    [Fact]
    public void TryMarkRefunded_WhenAlreadyRefunded_ShouldKeepFirstRefundDate()
    {
        var order = CreateSampleOrder();
        order.TryMarkPaymentReceived(DateTime.UtcNow);
        var firstRefund = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);
        order.TryMarkRefunded(firstRefund);

        var result = order.TryMarkRefunded(firstRefund.AddMinutes(5));

        result.Should().BeFalse();
        order.RefundedAt.Should().Be(firstRefund);
    }

    [Fact]
    public void TryMarkPaymentFailed_WhenPaymentReceived_ShouldReturnFalse()
    {
        var order = CreateSampleOrder();
        order.TryMarkPaymentReceived(DateTime.UtcNow);

        var result = order.TryMarkPaymentFailed(DateTime.UtcNow, null);

        result.Should().BeFalse();
        order.PaymentStatus.Should().Be(PaymentStatus.Alindi);
    }

    // --- Geçmiş ---

    [Fact]
    public void Order_WhenCreated_ShouldStartHistoryWithSiparisAlindi()
    {
        var order = CreateSampleOrder();

        order.History.Should().ContainSingle()
            .Which.Should().Match<OrderHistoryEntry>(h => h.Type == OrderHistoryType.SiparisAlindi && h.OccurredAt == order.CreatedDate);
    }

    [Fact]
    public void SuccessfulFlow_ShouldRecordEachStepInOrder()
    {
        var order = CreateSampleOrder();
        var at = DateTime.UtcNow;

        order.TryMarkStockReserved(at);
        order.TryMarkPaymentReceived(at);
        order.TrySetStatusToCompleted(at);

        order.History.Select(h => h.Type).Should().Equal(
            OrderHistoryType.SiparisAlindi, OrderHistoryType.StokAyrildi, OrderHistoryType.OdemeAlindi, OrderHistoryType.Tamamlandi);
    }

    // Durumu değiştirmeyen çağrı (kopya event) geçmişe kayıt eklememeli
    [Fact]
    public void DuplicateCalls_ShouldNotAddDuplicateHistory()
    {
        var order = CreateSampleOrder();
        var at = DateTime.UtcNow;

        order.TryMarkStockReserved(at);
        order.TryMarkStockReserved(at);
        order.TryMarkPaymentReceived(at);
        order.TryMarkPaymentReceived(at);
        order.TrySetStatusToCompleted(at);
        order.TrySetStatusToCanceled(at);

        order.History.Should().HaveCount(4);
    }

    [Fact]
    public void TryMarkStockFailed_ShouldKeepReasonAsNote()
    {
        var order = CreateSampleOrder();

        order.TryMarkStockFailed(DateTime.UtcNow, "Yetersiz stok");

        order.History.Last().Should().Match<OrderHistoryEntry>(h => h.Type == OrderHistoryType.StokYetersiz && h.Note == "Yetersiz stok");
    }

    // Ödeme stoktan önce işlenmişse stok sonucu tamamlanmış siparişe gelir ve yine kaydedilir
    [Fact]
    public void TryMarkStockReserved_AfterCompletion_ShouldRecord()
    {
        var order = CreateSampleOrder();
        order.TrySetStatusToCompleted(DateTime.UtcNow);

        order.TryMarkStockReserved(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void TryMarkStockReserved_AfterCancel_ShouldNotRecord()
    {
        var order = CreateSampleOrder();
        order.TrySetStatusToCanceled(DateTime.UtcNow, "Ödeme süresi doldu.");

        order.TryMarkStockReserved(DateTime.UtcNow).Should().BeFalse();
        order.History.Select(h => h.Type).Should().NotContain(OrderHistoryType.StokAyrildi);
    }
}