using FluentAssertions;
using Microsoft.AspNetCore.Hosting;

namespace EticaretMicroservice.Gateway.IntegrationTests;

public class CorsTests : IClassFixture<GatewayFactory>
{
    private readonly GatewayFactory _factory;

    public CorsTests(GatewayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preflight_FromDefaultLocalOrigin_IsAllowed()
    {
        var allowOrigin = await PreflightAllowOriginAsync(_factory.CreateClientWithOwnIp(), "http://localhost:5173");

        allowOrigin.Should().Be("http://localhost:5173");
    }

    [Fact]
    public async Task Preflight_WithConfiguredOrigins_AllowsConfiguredAndRejectsOthers()
    {
        using var configured = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Cors:AllowedOrigins:0", "https://shop.example.com"));
        var client = configured.CreateClient();

        (await PreflightAllowOriginAsync(client, "https://shop.example.com")).Should().Be("https://shop.example.com");
        (await PreflightAllowOriginAsync(client, "http://localhost:5173")).Should().BeNull();
    }

    private static async Task<string?> PreflightAllowOriginAsync(HttpClient client, string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/products");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request);
        return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;
    }
}
