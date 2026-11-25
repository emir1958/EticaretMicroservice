using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace EticaretMicroservice.Identity.IntegrationTests;

// Diğer servislerin yaptığı işi birebir yapar: token'ı yalnızca Identity'nin yayınladığı genel anahtarla doğrular.
public class JwksTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public JwksTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DiscoveryDocument_PointsToTheJwksEndpoint()
    {
        var document = await _client.GetFromJsonAsync<JsonElement>("/.well-known/openid-configuration");

        document.GetProperty("issuer").GetString().Should().Be("EticaretIdentityServer");
        document.GetProperty("jwks_uri").GetString().Should().EndWith("/.well-known/jwks.json");
    }

    [Fact]
    public async Task Jwks_PublishesOnlyThePublicPartOfASingleRs256Key()
    {
        var json = await _client.GetStringAsync("/.well-known/jwks.json");
        var key = JsonDocument.Parse(json).RootElement.GetProperty("keys").EnumerateArray().Single();

        key.GetProperty("kty").GetString().Should().Be("RSA");
        key.GetProperty("use").GetString().Should().Be("sig");
        key.GetProperty("alg").GetString().Should().Be("RS256");
        key.GetProperty("kid").GetString().Should().NotBeNullOrEmpty();
        key.GetProperty("n").GetString().Should().NotBeNullOrEmpty();
        key.GetProperty("e").GetString().Should().NotBeNullOrEmpty();

        // Özel anahtar parametreleri (RFC 7518: d, p, q, dp, dq, qi) asla yayınlanmamalı
        foreach (var privateField in new[] { "d", "p", "q", "dp", "dq", "qi" })
            key.TryGetProperty(privateField, out _).Should().BeFalse($"'{privateField}' özel anahtarın parçasıdır");
    }

    [Fact]
    public async Task IssuedToken_IsRs256SignedWithTheKidFromJwks_AndVerifiesAgainstIt()
    {
        var token = await RegisterAndLoginAsync();
        var jwks = new JsonWebKeySet(await _client.GetStringAsync("/.well-known/jwks.json"));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Header.Alg.Should().Be("RS256");
        jwt.Header.Kid.Should().Be(jwks.Keys.Single().Kid);

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            IssuerSigningKeys = jwks.GetSigningKeys(),
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
            ValidIssuer = "EticaretIdentityServer",
            ValidAudience = "EticaretGateway"
        }, out _);

        principal.Claims.Should().Contain(c => c.Type == "role" || c.Type.EndsWith("/role"));
    }

    [Fact]
    public async Task TamperedToken_FailsSignatureVerification()
    {
        var token = await RegisterAndLoginAsync();
        var jwks = new JsonWebKeySet(await _client.GetStringAsync("/.well-known/jwks.json"));

        // Payload'daki rol Admin yapılır, imza olduğu gibi bırakılır
        var parts = token.Split('.');
        var payload = Base64UrlEncoder.Decode(parts[1]).Replace("\"role\":\"User\"", "\"role\":\"Admin\"");
        var tampered = $"{parts[0]}.{Base64UrlEncoder.Encode(payload)}.{parts[2]}";
        payload.Should().Contain("Admin");

        var act = () => new JwtSecurityTokenHandler().ValidateToken(tampered, new TokenValidationParameters
        {
            IssuerSigningKeys = jwks.GetSigningKeys(),
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
            ValidIssuer = "EticaretIdentityServer",
            ValidAudience = "EticaretGateway"
        }, out _);

        act.Should().Throw<SecurityTokenInvalidSignatureException>();
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = $"jwks{Guid.NewGuid():N}@tests.local";
        await _client.PostAsJsonAsync("/api/auth/register", new { username = "tester", email, password = "Abcdef12" });
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Abcdef12" });
        return (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
    }

    private sealed record LoginResponse(string Token);
}
