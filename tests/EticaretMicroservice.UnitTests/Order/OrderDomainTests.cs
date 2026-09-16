using EticaretMicroservice.Services.Order.Domain.Entities;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Services.Order.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace EticaretMicroservice.UnitTests.Order;

public class OrderDomainTests
{
    private static Address CreateSampleAddress() =>
        new("İstanbul", "Kadıköy", "Moda Cad.", "34710", "No:1 Daire:5");

    [Fact]
    public void Order_WhenCreated_ShouldHaveBeklemedeStatus()
    {
        // Arrange & Act
        var order = new Services.Order.Domain.Entities.Order("buyer-123", CreateSampleAddress());

        // Assert
        order.OrderStatus.Should().Be(OrderStatus.Beklemede);
    }

    [Fact]
    public void TrySetStatusToCompleted_WhenStatusIsBeklemede_ShouldReturnTrueAndSetTamamlandi()
    {
        // Arrange
        var order = new Services.Order.Domain.Entities.Order("buyer-123", CreateSampleAddress());

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
        var order = new Services.Order.Domain.Entities.Order("buyer-123", CreateSampleAddress());
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
        var order = new Services.Order.Domain.Entities.Order("buyer-123", CreateSampleAddress());
        order.TrySetStatusToCompleted();

        // Act
        var result = order.TrySetStatusToCanceled();

        // Assert
        result.Should().BeFalse();
        order.OrderStatus.Should().Be(OrderStatus.Tamamlandı);
    }
}