using EticaretMicroservice.Shared.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EticaretMicroservice.UnitTests.Shared;

public class CorsExtensionsTests
{
    [Fact]
    public async Task WithoutConfiguration_AllowsLocalDevelopmentOrigins()
    {
        var policy = await GetPolicyAsync(new Dictionary<string, string?>());

        policy.Origins.Should().Contain("http://localhost:5173");
    }

    [Fact]
    public async Task WithConfiguredOrigins_AllowsOnlyThose()
    {
        var policy = await GetPolicyAsync(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://shop.example.com",
            ["Cors:AllowedOrigins:1"] = "https://admin.example.com"
        });

        policy.Origins.Should().BeEquivalentTo("https://shop.example.com", "https://admin.example.com");
    }

    [Fact]
    public async Task Policy_KeepsCredentialsAndCorrelationHeaderExposed()
    {
        var policy = await GetPolicyAsync(new Dictionary<string, string?>());

        policy.SupportsCredentials.Should().BeTrue();
        policy.ExposedHeaders.Should().Contain("X-Correlation-ID");
    }

    private static async Task<Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicy> GetPolicyAsync(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddSharedCors();

        await using var provider = services.BuildServiceProvider();
        var policy = await provider.GetRequiredService<ICorsPolicyProvider>()
            .GetPolicyAsync(new DefaultHttpContext(), CorsExtensions.PolicyName);

        return policy!;
    }
}
