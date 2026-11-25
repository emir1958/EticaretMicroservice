using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Dtos;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Shared.Exceptions;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using EticaretMicroservice.Services.Order.Application.Interfaces;
namespace EticaretMicroservice.IntegrationTests;

[Collection(OrderApiCollection.Name)]
public class CreateOrderIntegrationTests
{
    private readonly HttpClient _client;
    private readonly OrderApiFactory _factory;

    public CreateOrderIntegrationTests(OrderApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_WhenValidPayloadPosted_ShouldPersistOrderAndPublishEventWithCatalogPrice()
    {
        // Arrange
        var productId = "test-keyboard-001";
        var catalogPrice = 750.00m;
        var manipulatedClientPrice = 1.00m; // İstemcinin hileli göndermeye çalıştığı fiyat
        var buyerId = "integration-buyer-01"; // BuyerId gövdeden değil token'dan (test kimliğinden) alınır
        var correlationId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();

        // Catalog mock: Gerçek fiyatı 750 TL olan ürün
        _factory.CatalogRepoMock
            .Setup(x => x.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogProductDto(productId, "Mekanik Klavye", "Açıklama", catalogPrice));

        var command = new CreateOrderCommand
        {
            BuyerId = "body-buyer-should-be-ignored",
            Address = new AddressDto
            {
                ContactName = "Test Alıcı",
                City = "İstanbul",
                District = "Kadıköy",
                Street = "Bağdat Cad.",
                ZipCode = "34728",
                Line = "Daire: 10"
            },
            Payment = new PaymentDto
            {
                PaymentToken = "tok_test_integration_token"
            },
            OrderItems = new List<OrderItemDto>
            {
                new()
                {
                    ProductId = productId,
                    ProductName = "Manipüle Klavye Adı",
                    Price = manipulatedClientPrice,
                    Quantity = 2
                }
            }
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(command)
        };
        requestMessage.Headers.Add(TestAuthHandler.UserIdHeader, buyerId);
        requestMessage.Headers.Add("X-Correlation-ID", correlationId.ToString());
        requestMessage.Headers.Add("Idempotency-Key", idempotencyKey.ToString());

        // Act: API çağrısı
        var response = await _client.SendAsync(requestMessage);

        // Assert 1: HTTP Yanıt Kodu 200 OK olmalıdır
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert 2: Gerçek SQL Server tablolarının denetimi
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        // Sipariş tablosunda kayıt katalog fiyatıyla ve token'daki kullanıcıyla mı oluştu?
        var orderInDb = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.BuyerId == buyerId);

        orderInDb.Should().NotBeNull();
        orderInDb!.OrderItems.First().Price.Should().Be(catalogPrice);

        // Assert 3: Outbox üzerinden aşağı akışa (Stock) ulaşan event katalog fiyatını ve CorrelationId'yi taşıyor mu?
        // Outbox satırı teslim edildikten sonra silindiği için tablo yerine teslim edilen mesaj doğrulanır.
        var harness = _factory.Services.GetTestHarness();
        await OrderSagaTestDriver.WaitUntilAsync(
            () => Task.FromResult(harness.Consumed.Select<OrderCreatedEvent>().Any(m => m.Context.Message.OrderId == orderInDb.Id)),
            "OrderCreatedEvent aşağı akışa ulaşmalı");

        var delivered = harness.Consumed.Select<OrderCreatedEvent>()
            .Single(m => m.Context.Message.OrderId == orderInDb.Id).Context.Message;

        delivered.CorrelationId.Should().Be(orderInDb.CorrelationId, "saga anahtarı istemci header'ından değil siparişten gelir");
        delivered.CorrelationId.Should().NotBe(correlationId);
        delivered.BuyerId.Should().Be(buyerId);
        delivered.OrderItems.Should().ContainSingle(i => i.ProductId == productId && i.Price == catalogPrice);
    }

    [Fact]
    public async Task CreateOrder_WithProductMissingFromCatalog_Returns422AndSavesNothing()
    {
        var productId = "missing-product-422";
        var buyerId = "integration-buyer-422";

        _factory.CatalogRepoMock
            .Setup(x => x.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CatalogProductDto?)null);

        var response = await _client.SendAsync(CreateOrderRequest(buyerId, productId));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ReadDetailAsync(response)).Should().Contain(productId);
        await AssertNoOrderSavedAsync(buyerId);
    }

    [Fact]
    public async Task CreateOrder_WhenCatalogIsUnavailable_Returns503AndSavesNothing()
    {
        var productId = "product-catalog-down-503";
        var buyerId = "integration-buyer-503";

        _factory.CatalogRepoMock
            .Setup(x => x.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("Katalog servisine şu anda ulaşılamıyor."));

        var response = await _client.SendAsync(CreateOrderRequest(buyerId, productId));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await ReadDetailAsync(response)).Should().Be("Katalog servisine şu anda ulaşılamıyor.");
        await AssertNoOrderSavedAsync(buyerId);
    }

    [Fact]
    public async Task CreateOrder_RetriedWithSameKeyAfterFailure_IsProcessedAgain()
    {
        var productId = "product-retry-after-failure";
        var buyerId = "integration-buyer-retry";
        var idempotencyKey = Guid.NewGuid();

        // Catalog ilk çağrıda erişilemez, sonra düzelir
        _factory.CatalogRepoMock
            .SetupSequence(x => x.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("Katalog servisine şu anda ulaşılamıyor."))
            .ReturnsAsync(new CatalogProductDto(productId, "Ürün", "Açıklama", 100m));

        var first = await _client.SendAsync(CreateOrderRequest(buyerId, productId, idempotencyKey));
        first.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        // Başarısız istek anahtarı tüketmemeli: istemci aynı anahtarla tekrar deneyebilmeli
        var second = await _client.SendAsync(CreateOrderRequest(buyerId, productId, idempotencyKey));
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        (await dbContext.Orders.CountAsync(o => o.BuyerId == buyerId)).Should().Be(1);
    }

    [Fact]
    public async Task CreateOrder_RetriedWithSameKeyAfterSuccess_ReturnsFirstResponseWithoutSecondOrder()
    {
        var productId = "product-retry-after-success";
        var buyerId = "integration-buyer-replay";
        var idempotencyKey = Guid.NewGuid();

        _factory.CatalogRepoMock
            .Setup(x => x.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogProductDto(productId, "Ürün", "Açıklama", 100m));

        var first = await _client.SendAsync(CreateOrderRequest(buyerId, productId, idempotencyKey));
        var second = await _client.SendAsync(CreateOrderRequest(buyerId, productId, idempotencyKey));

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await second.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        (await dbContext.Orders.CountAsync(o => o.BuyerId == buyerId)).Should().Be(1);
    }

    private static HttpRequestMessage CreateOrderRequest(string buyerId, string productId, Guid? idempotencyKey = null)
    {
        var command = new CreateOrderCommand
        {
            Address = new AddressDto
            {
                ContactName = "Test Alıcı",
                City = "İstanbul",
                District = "Kadıköy",
                Street = "Bağdat Cad.",
                Line = "Daire: 10"
            },
            Payment = new PaymentDto { PaymentToken = "tok_test_integration_token" },
            OrderItems = new List<OrderItemDto> { new() { ProductId = productId, ProductName = "Test Ürün", Price = 1, Quantity = 1 } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders") { Content = JsonContent.Create(command) };
        request.Headers.Add(TestAuthHandler.UserIdHeader, buyerId);
        request.Headers.Add("Idempotency-Key", (idempotencyKey ?? Guid.NewGuid()).ToString());
        return request;
    }

    private static async Task<string?> ReadDetailAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("detail").GetString();
    }

    private async Task AssertNoOrderSavedAsync(string buyerId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        (await dbContext.Orders.AnyAsync(o => o.BuyerId == buyerId)).Should().BeFalse();
    }
}
