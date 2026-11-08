using System.Net.Http.Json;
using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Dtos;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Application.Sagas;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Services.Order.Domain.ValueObjects;
using EticaretMicroservice.Services.Order.Infrastructure.BackgroundServices;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
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

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrderSagaTestDriver(OrderApiFactory factory) : this(factory, factory.CatalogRepoMock)
    {
    }

    // Timeout senaryoları için: OrderApiFactory.ShortTimeoutApi (aynı DB, kendi bus'ı ve harness'ı)
    public static OrderSagaTestDriver WithShortTimeout(OrderApiFactory factory) =>
        new(factory.ShortTimeoutApi, factory.CatalogRepoMock);

    private OrderSagaTestDriver(WebApplicationFactory<Program> factory, Mock<ICatalogRepository> catalogRepoMock)
    {
        _factory = factory;
        _client = factory.CreateClient();

        catalogRepoMock
            .Setup(x => x.GetProductByIdAsync(It.Is<string>(id => id.StartsWith(ProductPrefix)), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new CatalogProductDto(id, "Saga Test Ürünü", "Açıklama", ProductPrice));
    }

    public ITestHarness Harness => _factory.Services.GetTestHarness();

    public async Task<CreatedOrder> CreateOrderAsync(int quantity = 1, string? buyerId = null)
    {
        buyerId ??= $"saga-buyer-{Guid.NewGuid():N}";

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

    // Order satırını OrderCreated yayınlamadan (outbox'a dokunmadan) yazar: saga'yı test kendisi başlatır.
    // Saga'nın timeout'u gerçek bir siparişi iptal edebilsin diye sahte OrderId yerine kullanılır.
    public async Task<CreatedOrder> CreateOrderWithoutSagaAsync()
    {
        var buyerId = $"saga-buyer-{Guid.NewGuid():N}";
        var order = new Services.Order.Domain.Entities.Order(buyerId, new Address("İstanbul", "Kadıköy", "Moda Cad.", "34710", "No:1"), Guid.NewGuid());
        order.AddOrderItem($"{ProductPrefix}{Guid.NewGuid():N}", "Saga Test Ürünü", ProductPrice, 1);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        return new CreatedOrder(order.Id, order.CorrelationId, buyerId);
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

    // Zamanlayıcı dışından gelen (token'sız) timeout mesajı: erken/başıboş teslimi simüle eder
    public Task PaymentTimeoutAsync(CreatedOrder order) =>
        Harness.Bus.Publish(new PaymentTimeoutExpired { CorrelationId = order.CorrelationId });

    public async Task<OrderStatus> GetStatusAsync(int orderId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        return await dbContext.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => o.OrderStatus)
            .SingleAsync();
    }

    public Task OrderCreatedAsync(CreatedOrder order) =>
        Harness.Bus.Publish(new OrderCreatedEvent
        {
            CorrelationId = order.CorrelationId,
            OrderId = order.OrderId,
            BuyerId = order.BuyerId,
            PaymentToken = "tok_saga_test"
        });

    public async Task<OrderState?> GetSagaAsync(Guid correlationId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        return await dbContext.Set<OrderState>().AsNoTracking().SingleOrDefaultAsync(s => s.CorrelationId == correlationId);
    }

    public OrderTimeoutSweeper Sweeper => _factory.Services.GetRequiredService<OrderTimeoutSweeper>();

    // Zamanlanmış timeout mesajının kaybolduğu durumu simüle eder: instance'ın son tarihi geçmişe çekilir
    public async Task SetSagaExpiresAtAsync(Guid correlationId, DateTime expiresAt)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await dbContext.Set<OrderState>()
            .Where(s => s.CorrelationId == correlationId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, expiresAt));
    }

    public async Task<int> CountSagasForOrderAsync(int orderId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        return await dbContext.Set<OrderState>().CountAsync(s => s.OrderId == orderId);
    }

    public Task WaitForSagaStateAsync(CreatedOrder order, string expectedState) =>
        WaitUntilAsync(async () => (await GetSagaAsync(order.CorrelationId))?.CurrentState == expectedState,
            $"OrderId {order.OrderId} saga durumu '{expectedState}' olmalı");

    public int ReceivedDownstreamCount<T>(Func<T, bool> filter) where T : class =>
        Harness.Consumed.Select<T>().Count(m => filter(m.Context.Message));

    // Harness.Consumed kayıtları MessageId ile tutar: aynı mesajı hem saga hem sink tükettiğinde tek kayıt kalır.
    // Saga'nın kaç kez başarıyla işlediğini saymak için bus'a bir consume observer bağlanır.
    public SagaConsumeCounter<T> CountSagaConsumes<T>(Func<T, bool> filter) where T : class
    {
        var counter = new SagaConsumeCounter<T>(filter);
        Harness.Bus.ConnectConsumeMessageObserver(counter);
        return counter;
    }

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
