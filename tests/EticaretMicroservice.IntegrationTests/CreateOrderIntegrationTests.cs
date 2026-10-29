using System.Net;
using System.Net.Http.Json;
using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Dtos;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using EticaretMicroservice.Shared.Events;
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
            CorrelationId = correlationId,
            BuyerId = "body-buyer-should-be-ignored",
            Address = new AddressDto
            {
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

        delivered.CorrelationId.Should().Be(correlationId);
        delivered.BuyerId.Should().Be(buyerId);
        delivered.OrderItems.Should().ContainSingle(i => i.ProductId == productId && i.Price == catalogPrice);
    }
}
