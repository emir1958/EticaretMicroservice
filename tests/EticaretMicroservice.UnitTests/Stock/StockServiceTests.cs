using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Models;
using EticaretMicroservice.Stock.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EticaretMicroservice.UnitTests.Stock;

public class StockServiceTests
{
    private static DbContextOptions<StockDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<StockDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private static ILogger<StockService> CreateMockLogger() =>
        new Mock<ILogger<StockService>>().Object;

    [Fact]
    public async Task ReserveStockAsync_WhenSufficientStockExists_ShouldDecreaseAvailableAndIncreaseReserved()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var productId = Guid.NewGuid().ToString();

        await using (var context = new StockDbContext(options))
        {
            context.ProductStocks.Add(new ProductStock
            {
                ProductId = productId,
                AvailableStock = 10,
                ReservedStock = 0
            });
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new StockDbContext(options))
        {
            var service = new StockService(context, CreateMockLogger());

            var result = await service.ReserveStockAsync(productId, 3);

            // Assert
            result.Should().BeTrue();
            var stock = await context.ProductStocks.FirstAsync(x => x.ProductId == productId);
            stock.AvailableStock.Should().Be(7);
            stock.ReservedStock.Should().Be(3);
        }
    }

    [Fact]
    public async Task ReserveStockAsync_WhenStockIsInsufficient_ShouldReturnFalseAndNotAlterStock()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var productId = Guid.NewGuid().ToString();

        await using (var context = new StockDbContext(options))
        {
            context.ProductStocks.Add(new ProductStock
            {
                ProductId = productId,
                AvailableStock = 2,
                ReservedStock = 0
            });
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new StockDbContext(options))
        {
            var service = new StockService(context, CreateMockLogger());

            var result = await service.ReserveStockAsync(productId, 5);

            // Assert
            result.Should().BeFalse();
            var stock = await context.ProductStocks.FirstAsync(x => x.ProductId == productId);
            stock.AvailableStock.Should().Be(2);
            stock.ReservedStock.Should().Be(0);
        }
    }

    [Fact]
    public async Task ReleaseStockAsync_ShouldReturnReservedStockBackToAvailable()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var productId = Guid.NewGuid().ToString();

        await using (var context = new StockDbContext(options))
        {
            context.ProductStocks.Add(new ProductStock
            {
                ProductId = productId,
                AvailableStock = 5,
                ReservedStock = 5
            });
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new StockDbContext(options))
        {
            var service = new StockService(context, CreateMockLogger());

            await service.ReleaseStockAsync(productId, 5);

            // Assert
            var stock = await context.ProductStocks.FirstAsync(x => x.ProductId == productId);
            stock.AvailableStock.Should().Be(10);
            stock.ReservedStock.Should().Be(0);
        }
    }
}