using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Consumers;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Services;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EticaretMicroservice.UnitTests.Stock;

public class OrderCreatedEventConsumerTests
{
    [Fact]
    public async Task Consume_WhenStockReservationSucceeds_ShouldPublishStockReservedEvent()
    {
        // Arrange
        var stockServiceMock = new Mock<IStockService>();
        var loggerMock = new Mock<ILogger<OrderCreatedEventConsumer>>();

        stockServiceMock
            .Setup(s => s.ReserveForOrderAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<OrderItemMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReservationResult.Reserved);

        var dbOptions = new DbContextOptionsBuilder<StockDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var provider = new ServiceCollection()
            .AddScoped(_ => new StockDbContext(dbOptions)) // Consumer'ın ihtiyaç duyduğu DbContext
            .AddSingleton(stockServiceMock.Object)
            .AddSingleton(loggerMock.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<OrderCreatedEventConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderCreatedEvent = new OrderCreatedEvent
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 101,
            BuyerId = "user-123",
            PaymentToken = "tok_visa_valid",
            OrderItems = new List<OrderItemMessage>
            {
                new() { ProductId = "prod-abc", Quantity = 2, Price = 100m }
            }
        };

        // Act
        await harness.Bus.Publish(orderCreatedEvent);

        // Assert
        var consumerHarness = harness.GetConsumerHarness<OrderCreatedEventConsumer>();
        (await consumerHarness.Consumed.Any<OrderCreatedEvent>()).Should().BeTrue();

        stockServiceMock.Verify(
            s => s.ReserveForOrderAsync(
                101,
                It.Is<IReadOnlyCollection<OrderItemMessage>>(items =>
                    items.Count == 1 && items.Single().ProductId == "prod-abc" && items.Single().Quantity == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);

        (await harness.Published.Any<StockReservedEvent>()).Should().BeTrue();
        (await harness.Published.Any<StockFailedEvent>()).Should().BeFalse();
    }

    [Fact]
    public async Task Consume_WhenStockReservationFails_ShouldPublishStockFailedEvent()
    {
        // Arrange
        var stockServiceMock = new Mock<IStockService>();
        var loggerMock = new Mock<ILogger<OrderCreatedEventConsumer>>();

        stockServiceMock
            .Setup(s => s.ReserveForOrderAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<OrderItemMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReservationResult.InsufficientStock);

        var dbOptions = new DbContextOptionsBuilder<StockDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var provider = new ServiceCollection()
            .AddScoped(_ => new StockDbContext(dbOptions)) // Consumer'ın ihtiyaç duyduğu DbContext
            .AddSingleton(stockServiceMock.Object)
            .AddSingleton(loggerMock.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<OrderCreatedEventConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderCreatedEvent = new OrderCreatedEvent
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 102,
            BuyerId = "user-456",
            PaymentToken = "tok_visa_valid",
            OrderItems = new List<OrderItemMessage>
            {
                new() { ProductId = "out-of-stock-prod", Quantity = 10, Price = 50m }
            }
        };

        // Act
        await harness.Bus.Publish(orderCreatedEvent);

        // Assert
        var consumerHarness = harness.GetConsumerHarness<OrderCreatedEventConsumer>();
        (await consumerHarness.Consumed.Any<OrderCreatedEvent>()).Should().BeTrue();

        (await harness.Published.Any<StockFailedEvent>()).Should().BeTrue();
        (await harness.Published.Any<StockReservedEvent>()).Should().BeFalse();
    }
}