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
        var result = order.TrySetStatusToCompleted();

        // Assert
        result.Should().BeTrue();
        order.OrderStatus.Should().Be(OrderStatus.Tamamlandı);
    }

    [Fact]
    public void TrySetStatusToCompleted_WhenOrderIsAlreadyCanceled_ShouldReturnFalseAndKeepIptalEdildi()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.TrySetStatusToCanceled();

        // Act
        var result = order.TrySetStatusToCompleted();

        // Assert
        result.Should().BeFalse();
        order.OrderStatus.Should().Be(OrderStatus.IptalEdildi);
    }

    [Fact]
    public void TrySetStatusToCanceled_WhenOrderIsAlreadyCompleted_ShouldReturnFalseAndKeepTamamlandi()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.TrySetStatusToCompleted();

        // Act
        var result = order.TrySetStatusToCanceled();

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
        order.TryMarkPaymentReceived();
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
        order.TryMarkPaymentReceived();
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
        order.TryMarkPaymentReceived();

        var result = order.TryMarkPaymentFailed();

        result.Should().BeFalse();
        order.PaymentStatus.Should().Be(PaymentStatus.Alindi);
    }
}