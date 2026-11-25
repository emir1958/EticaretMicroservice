using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Dtos;
using EticaretMicroservice.Services.Order.Application.Handlers;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Shared.Exceptions;
using FluentAssertions;
using MassTransit;
using Moq;
using Xunit;

namespace EticaretMicroservice.UnitTests.Order;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ICatalogRepository> _catalogRepoMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _catalogRepoMock = new Mock<ICatalogRepository>();

        // Handler kayıt adımlarını ExecuteInTransactionAsync içine alır. Mock'lanmış bir metot verilen
        // delegate'i kendiliğinden çalıştırmaz (default(int) = 0 döner); gerçek repository gibi davranması için
        // transaction içindeki iş çalıştırılıp sonucu döndürülür.
        _orderRepoMock
            .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<int>> action, CancellationToken _) => action());

        _handler = new CreateOrderCommandHandler(
            _orderRepoMock.Object,
            _publishEndpointMock.Object,
            _catalogRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenClientSendsManipulatedPrice_ShouldOverrideWithCatalogPriceAndPublishVerifiedEvent()
    {
        // Arrange
        var productId = "prod-test-101";
        var catalogProductName = "Kablosuz Mouse";
        var catalogPrice = 250m;
        var manipulatedClientPrice = 1m;
        var quantity = 2;
        var expectedOrderId = 42;
        Services.Order.Domain.Entities.Order? savedOrder = null;

        var command = new CreateOrderCommand
        {
            BuyerId = "buyer-123",
            Address = new AddressDto
            {
                ContactName = "Test Alıcı",
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
                    ProductName = "Sahte Ürün Adı",
                    Price = manipulatedClientPrice,
                    Quantity = quantity
                }
            }
        };

        var mockCatalogProduct = new CatalogProductDto(productId, catalogProductName, "Açıklama", catalogPrice);
        _catalogRepoMock
            .Setup(c => c.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCatalogProduct);

        // Veritabanının IDENTITY ataması taklit edilir
        _orderRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Services.Order.Domain.Entities.Order>()))
            .ReturnsAsync((Services.Order.Domain.Entities.Order order) =>
            {
                typeof(Services.Order.Domain.Entities.Order)
                    .GetProperty("Id")?
                    .SetValue(order, expectedOrderId);
                savedOrder = order;
                return order;
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedOrderId);

        // CorrelationId sunucuda üretilir: boş olmamalı ve siparişe yazılanla event'teki aynı olmalı (saga eşleştirmesi)
        savedOrder.Should().NotBeNull();
        savedOrder!.CorrelationId.Should().NotBe(Guid.Empty);

        // Siparişe istemcinin fiyatı (1 TL) değil katalog fiyatı (250 TL) yazılır
        _orderRepoMock.Verify(
            repo => repo.AddAsync(It.Is<Services.Order.Domain.Entities.Order>(o =>
                o.BuyerId == "buyer-123" &&
                o.OrderItems.Count == 1 &&
                o.OrderItems.Any(item =>
                    item.ProductId == productId &&
                    item.ProductName == catalogProductName &&
                    item.Price == catalogPrice &&
                    item.Quantity == quantity)
            )),
            Times.Once);

        // Outbox'a yazılan event de katalog adını/fiyatını ve siparişin CorrelationId değerini taşır
        _publishEndpointMock.Verify(
            p => p.Publish(It.Is<OrderCreatedEvent>(e =>
                e.CorrelationId == savedOrder!.CorrelationId &&
                e.OrderId == expectedOrderId &&
                e.BuyerId == "buyer-123" &&
                e.PaymentToken == "tok_test_abc123" &&
                e.OrderItems.Count == 1 &&
                e.OrderItems.Any(i =>
                    i.ProductId == productId &&
                    i.ProductName == catalogProductName &&
                    i.Price == catalogPrice &&
                    i.Quantity == quantity)
            ), It.IsAny<CancellationToken>()),
            Times.Once);

        // Biri IDENTITY için, biri outbox için
        _orderRepoMock.Verify(
            repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenProductNotFoundInCatalog_ShouldThrowBusinessRuleExceptionAndNotSave()
    {
        // Arrange
        var nonExistingProductId = "invalid-prod-999";
        var command = new CreateOrderCommand
        {
            BuyerId = "buyer-123",
            Address = new AddressDto
            {
                ContactName = "Test Alıcı",
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

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
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