using System.Net;
using System.Net.Http.Json;
using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Application.Dtos;
using FluentAssertions;

namespace EticaretMicroservice.IntegrationTests;

// Müşterinin kendi siparişlerini görüntülemesi (Siparişlerim / sipariş detayı).
// Asıl risk IDOR: bir kullanıcı id'yi değiştirerek başkasının siparişini görebilir mi?
[Collection(OrderApiCollection.Name)]
public class CustomerOrderQueryTests
{
    private readonly OrderApiFactory _factory;
    private readonly OrderSagaTestDriver _driver;

    public CustomerOrderQueryTests(OrderApiFactory factory)
    {
        _factory = factory;
        _driver = new OrderSagaTestDriver(factory);
    }

    [Fact]
    public async Task GetOwnOrderDetail_ReturnsOrderWithItems()
    {
        var order = await _driver.CreateOrderAsync(quantity: 2);

        var response = await GetAsync($"/api/orders/user/{order.OrderId}", order.BuyerId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<OrderDto>();
        dto!.Id.Should().Be(order.OrderId);
        dto.OrderItems.Should().ContainSingle().Which.Quantity.Should().Be(2);
        dto.TotalPrice.Should().Be(OrderSagaTestDriver.ProductPrice * 2);
    }

    // Var olan ama başkasına ait sipariş, hiç olmayan siparişle aynı yanıtı almalı
    [Fact]
    public async Task GetOtherUsersOrderDetail_ReturnsNotFound()
    {
        var order = await _driver.CreateOrderAsync();

        var response = await GetAsync($"/api/orders/user/{order.OrderId}", "another-buyer");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetNonExistingOrderDetail_ReturnsNotFound()
    {
        var response = await GetAsync($"/api/orders/user/{int.MaxValue}", "any-buyer");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderDetail_WithoutToken_ReturnsUnauthorized()
    {
        var order = await _driver.CreateOrderAsync();

        var response = await GetAsync($"/api/orders/user/{order.OrderId}", buyerId: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOwnOrders_ReturnsOnlyOwnOrders_NewestFirst()
    {
        var buyerId = $"saga-buyer-{Guid.NewGuid():N}";
        var older = await _driver.CreateOrderAsync(buyerId: buyerId);
        var newer = await _driver.CreateOrderAsync(buyerId: buyerId);
        await _driver.CreateOrderAsync(); // başka bir alıcının siparişi listede olmamalı

        var response = await GetAsync("/api/orders/user", buyerId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        orders!.Select(o => o.Id).Should().Equal(newer.OrderId, older.OrderId);
    }

    private async Task<HttpResponseMessage> GetAsync(string url, string? buyerId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (buyerId != null)
            request.Headers.Add(TestAuthHandler.UserIdHeader, buyerId);

        return await _factory.CreateClient().SendAsync(request);
    }
}
