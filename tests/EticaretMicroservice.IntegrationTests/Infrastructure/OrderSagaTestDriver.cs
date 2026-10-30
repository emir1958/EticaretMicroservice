using System.Net.Http.Json;
using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Dtos;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EticaretMicroservice.IntegrationTests.Infrastructure;

public record CreatedOrder(int OrderId, Guid CorrelationId, string BuyerId);

// Saga testlerinin ortak adımları: HTTP ile sipariş oluşturma, diğer servislerin event'lerini simüle etme,
// sipariş durumunu ve aşağı akışa ulaşan mesajları okuma.
public class OrderSagaTestDriver
{
    public const string ProductPrefix = "saga-product-";
    public const decimal ProductPrice = 100m;

    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(2);

    private readonly OrderApiFactory _factory;
    private readonly HttpClient _client;

    public OrderSagaTestDriver(OrderApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        _factory.CatalogRepoMock
            .Setup(x => x.GetProductByIdAsync(It.Is<string>(id => id.StartsWith(ProductPrefix)), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new CatalogProductDto(id, "Saga Test Ürünü", "Açıklama", ProductPrice));
    }

    public ITestHarness Harness => _factory.Services.GetTestHarness();

    public async Task<CreatedOrder> CreateOrderAsync(int quantity = 1)
    {
        var buyerId = $"saga-buyer-{Guid.NewGuid():N}";

        var command = new CreateOrderCommand
        {
            Address = new AddressDto { City = "İstanbul", District = "Kadıköy", Street = "Moda Cad.", ZipCode = "34710", Line = "No:1" },
            Payment = new PaymentDto { PaymentToken = "tok_saga_test" },
            OrderItems = new List<OrderItemDto>
            {
                new() { ProductId = $"{ProductPrefix}{Guid.NewGuid():N}", ProductName = "Saga Test Ürünü", Price = ProductPrice, Quantity = quantity }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders") { Content = JsonContent.Create(command) };
        request.Headers.Add(TestAuthHandler.UserIdHeader, buyerId);
        request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();

        // Saga anahtarı sunucuda üretilir; diğer servislerin event'leri bu değeri taşır
        return new CreatedOrder(body!.OrderId, await GetCorrelationIdAsync(body.OrderId), buyerId);
    }

    public async Task<Guid> GetCorrelationIdAsync(int orderId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        return await dbContext.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => o.CorrelationId)
            .SingleAsync();
    }

    public Task StockReservedAsync(CreatedOrder order) =>
        Harness.Bus.Publish(new StockReservedEvent
        {
            CorrelationId = order.CorrelationId,
            OrderId = order.OrderId,
            BuyerId = order.BuyerId,
            TotalPrice = ProductPrice,
            PaymentToken = "tok_saga_test"
        });

    public Task StockFailedAsync(CreatedOrder order) =>
        Harness.Bus.Publish(new StockFailedEvent
        {
            CorrelationId = order.CorrelationId,
            OrderId = order.OrderId,
            BuyerId = order.BuyerId,
            Message = "Yetersiz stok"
        });

    public Task PaymentCompletedAsync(CreatedOrder order) =>
        Harness.Bus.Publish(new PaymentCompletedEvent
        {
            CorrelationId = order.CorrelationId,
            OrderId = order.OrderId,
            BuyerId = order.BuyerId
        });

    public Task PaymentFailedAsync(CreatedOrder order) =>
        Harness.Bus.Publish(new PaymentFailedEvent
        {
            CorrelationId = order.CorrelationId,
            OrderId = order.OrderId,
            BuyerId = order.BuyerId,
            Message = "Kart reddedildi"
        });

    public async Task<OrderStatus> GetStatusAsync(int orderId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        return await dbContext.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => o.OrderStatus)
            .SingleAsync();
    }

    public int ReceivedDownstreamCount<T>(Func<T, bool> filter) where T : class =>
        Harness.Consumed.Select<T>().Count(m => filter(m.Context.Message));

    public Task WaitForStatusAsync(CreatedOrder order, OrderStatus expected) =>
        WaitUntilAsync(async () => await GetStatusAsync(order.OrderId) == expected,
            $"OrderId {order.OrderId} durumu '{expected}' olmalı");

    public static async Task WaitUntilAsync(Func<Task<bool>> condition, string description)
    {
        var deadline = DateTime.UtcNow + WaitTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(100);
        }

        throw new TimeoutException($"Beklenen durum {WaitTimeout.TotalSeconds} sn içinde oluşmadı: {description}");
    }

    public static Task SettleAsync() => Task.Delay(SettleDelay);

    private record CreateOrderResponse(int OrderId);
}
