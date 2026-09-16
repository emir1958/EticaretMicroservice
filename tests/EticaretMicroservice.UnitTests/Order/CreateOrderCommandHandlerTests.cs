using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Dtos;
using EticaretMicroservice.Services.Order.Application.Handlers;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace EticaretMicroservice.UnitTests.Order;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ICatalogRepository> _catalogRepoMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _catalogRepoMock = new Mock<ICatalogRepository>();

        _handler = new CreateOrderCommandHandler(
            _orderRepoMock.Object,
            _publishEndpointMock.Object,
            _httpContextAccessorMock.Object,
            _catalogRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidCommandProvided_ShouldFetchCatalogProductAndPublishOrderCreatedEvent()
    {
        // Arrange
        var productId = "prod-test-101";
        var productName = "Kablosuz Mouse";
        var productPrice = 250m;
        var quantity = 2;
        var expectedOrderId = 42;

        var command = new CreateOrderCommand
        {
            BuyerId = "buyer-123",
            Address = new AddressDto
            {
                City = "İstanbul",
                District = "Kadıköy",
                Street = "Moda",
                ZipCode = "34710",
                Line = "No:1 Daire:5"
            },
            Payment = new PaymentDto
            {
                PaymentToken = "tok_test_abc123"
            },
            OrderItems = new List<OrderItemDto>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = quantity
                }
            }
        };

        // 1. Catalog API Mock: Ürün ve fiyat bilgisi dönüyor
        var mockCatalogProduct = new CatalogProductDto(productId, productName, "Açıklama", productPrice);
        _catalogRepoMock
            .Setup(c => c.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCatalogProduct);

        // 2. Order Repository Mock: AddAsync çağrıldığında siparişi simüle edilmiş ID ile döndür
        _orderRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Services.Order.Domain.Entities.Order>()))
            .ReturnsAsync((Services.Order.Domain.Entities.Order order) =>
            {
                typeof(Services.Order.Domain.Entities.Order)
                    .GetProperty("Id")?
                    .SetValue(order, expectedOrderId);
                return order;
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedOrderId);

        // Catalog'dan ürünün doğru ID ile sorgulandığını doğrula
        _catalogRepoMock.Verify(
            c => c.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);

        // Veritabanına ürünün catalog fiyatı ve ismiyle eklendiğini doğrula
        _orderRepoMock.Verify(
            repo => repo.AddAsync(It.Is<Services.Order.Domain.Entities.Order>(o =>
                o.BuyerId == "buyer-123" &&
                o.OrderItems.Any(item =>
                    item.ProductId == productId &&
                    item.ProductName == productName &&
                    item.Price == productPrice &&
                    item.Quantity == quantity)
            )),
            Times.Once);

        // Event Bus'a OrderCreatedEvent basıldığını doğrula
        _publishEndpointMock.Verify(
            p => p.Publish(It.Is<OrderCreatedEvent>(e =>
                e.OrderId == expectedOrderId &&
                e.BuyerId == "buyer-123" &&
                e.PaymentToken == "tok_test_abc123" &&
                e.OrderItems.Any(i => i.ProductId == productId && i.Price == productPrice && i.Quantity == quantity)
            ), It.IsAny<CancellationToken>()),
            Times.Once);

        // DB SaveChanges çağrısını doğrula
        _orderRepoMock.Verify(
            repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductNotFoundInCatalog_ShouldThrowInvalidOperationExceptionAndNotSave()
    {
        // Arrange: Katalogda olmayan sahte bir ürün ID'si
        var nonExistingProductId = "invalid-prod-999";
        var command = new CreateOrderCommand
        {
            BuyerId = "buyer-123",
            Address = new AddressDto
            {
                City = "İstanbul",
                District = "Kadıköy",
                Street = "Moda",
                ZipCode = "34710",
                Line = "No:1"
            },
            Payment = new PaymentDto { PaymentToken = "token" },
            OrderItems = new List<OrderItemDto>
            {
                new() { ProductId = nonExistingProductId, Quantity = 1 }
            }
        };

        _catalogRepoMock
            .Setup(c => c.GetProductByIdAsync(nonExistingProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CatalogProductDto?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert: Exception fırlatmalı, event atmamalı ve veritabanına kayıt yapmamalı
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{nonExistingProductId}*");

        _orderRepoMock.Verify(
            repo => repo.AddAsync(It.IsAny<Services.Order.Domain.Entities.Order>()),
            Times.Never);

        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _orderRepoMock.Verify(
            repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}