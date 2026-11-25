using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using EticaretMicroservice.Identity.Api.Persistence;
using EticaretMicroservice.Shared.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EticaretMicroservice.Identity.IntegrationTests;

public class RefreshTokenTests : IClassFixture<IdentityApiFactory>
{
    private const string Password = "Abcdef12";
    private const string CookieName = "refresh_token";
    private const string RefreshAudience = "EticaretIdentityRefresh";

    private readonly IdentityApiFactory _factory;
    private readonly HttpClient _client;

    public RefreshTokenTests(IdentityApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_SetsHardenedRefreshCookie_AndShortLivedAccessToken()
    {
        var (response, accessToken, _) = await RegisterAndLoginAsync();

        var attributes = GetRefreshSetCookie(response)!.ToLowerInvariant();
        attributes.Should().Contain("httponly");
        attributes.Should().Contain("secure");
        attributes.Should().Contain("samesite=strict");
        attributes.Should().Contain("path=/api/auth");

        // Access token yaklaşık 15 dk geçerlidir (7 gün değil)
        var lifetime = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).ValidTo - DateTime.UtcNow;
        lifetime.Should().BeGreaterThan(TimeSpan.FromMinutes(13)).And.BeLessThan(TimeSpan.FromMinutes(16));
    }

    [Fact]
    public async Task Refresh_ReturnsNewAccessToken_AndRenewsTheCookie()
    {
        var (loginResponse, _, _) = await RegisterAndLoginAsync();

        var refresh = await RefreshAsync(ExtractRefreshToken(loginResponse));

        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTokenAsync(refresh)).Should().NotBeNullOrEmpty();
        GetRefreshSetCookie(refresh).Should().NotBeNull("oturum süresi kullanıldıkça uzar");
    }

    [Fact]
    public async Task Refresh_WithoutOrWithGarbageCookie_Returns401()
    {
        (await RefreshAsync(null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync("not-a-real-token")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReadsTheCurrentRoleFromTheDatabase()
    {
        var (loginResponse, _, email) = await RegisterAndLoginAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
            await db.Users.Where(u => u.Email == email).ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, "Admin"));
        }

        var refreshed = await ReadTokenAsync(await RefreshAsync(ExtractRefreshToken(loginResponse)));

        new JwtSecurityTokenHandler().ReadJwtToken(refreshed).Claims
            .Should().Contain(c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public async Task Logout_InvalidatesTheRefreshToken_AndClearsTheCookie()
    {
        var (loginResponse, _, _) = await RegisterAndLoginAsync();
        var refreshToken = ExtractRefreshToken(loginResponse);

        var logout = await SendWithCookieAsync("/api/auth/logout", refreshToken);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        GetRefreshSetCookie(logout)!.ToLowerInvariant().Should().Contain("expires=");
        (await RefreshAsync(refreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_InvalidatesTheUsersOtherSessionsToo()
    {
        var (firstLogin, _, email) = await RegisterAndLoginAsync();
        var secondLogin = await LoginAsync(email);

        await SendWithCookieAsync("/api/auth/logout", ExtractRefreshToken(firstLogin));

        // Basit tasarımın bilinçli sonucu: çıkış, kullanıcının tüm cihazlarındaki oturumları kapatır
        (await RefreshAsync(ExtractRefreshToken(secondLogin))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithAnAlreadyRevokedToken_DoesNotKillANewerSession()
    {
        var (firstLogin, _, email) = await RegisterAndLoginAsync();
        var oldToken = ExtractRefreshToken(firstLogin);
        await SendWithCookieAsync("/api/auth/logout", oldToken);

        var newerLogin = await LoginAsync(email);
        await SendWithCookieAsync("/api/auth/logout", oldToken); // eski, zaten iptal edilmiş token tekrar çıkış yapmaya çalışır

        (await RefreshAsync(ExtractRefreshToken(newerLogin))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_Returns401()
    {
        var (_, _, email) = await RegisterAndLoginAsync();
        var expired = await CraftRefreshTokenAsync(email, expires: DateTime.UtcNow.AddMinutes(-1));

        (await RefreshAsync(expired)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithTokenSignedByAnotherKey_Returns401()
    {
        var (_, _, email) = await RegisterAndLoginAsync();
        var forged = await CraftRefreshTokenAsync(email, signingKey: new RsaSecurityKey(RSA.Create(2048)));

        (await RefreshAsync(forged)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithStaleTokenVersion_Returns401()
    {
        var (_, _, email) = await RegisterAndLoginAsync();
        var stale = await CraftRefreshTokenAsync(email, versionOffset: -1);

        (await RefreshAsync(stale)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnAccessToken_CannotBeUsedAsARefreshToken()
    {
        var (_, accessToken, _) = await RegisterAndLoginAsync();

        (await RefreshAsync(accessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ARefreshToken_CannotBeUsedAsAnAccessToken_EvenForAnAdmin()
    {
        // Refresh token Identity'nin gerçek anahtarıyla imzalıdır; yalnızca farklı "aud" taşıdığı için API'de geçmemeli
        var adminLogin = await LoginAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword);
        var refreshToken = ExtractRefreshToken(adminLogin);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

        (await _client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string NewEmail() => $"user{Guid.NewGuid():N}@tests.local";

    private async Task<(HttpResponseMessage Response, string AccessToken, string Email)> RegisterAndLoginAsync()
    {
        var email = NewEmail();
        (await _client.PostAsJsonAsync("/api/auth/register", new { username = "tester", email, password = Password }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await LoginAsync(email);
        return (login, await ReadTokenAsync(login), email);
    }

    private async Task<HttpResponseMessage> LoginAsync(string email, string password = Password)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        return login;
    }

    // Identity'nin gerçek anahtarıyla (ya da verilen başka bir anahtarla) istenen özelliklerde refresh token üretir
    private async Task<string> CraftRefreshTokenAsync(string email, DateTime? expires = null, SecurityKey? signingKey = null, int versionOffset = 0)
    {
        string userId;
        int version;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);
            (userId, version) = (user.Id, user.TokenVersion);
        }

        var handler = new JwtSecurityTokenHandler();
        var now = DateTime.UtcNow;
        var token = handler.CreateJwtSecurityToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("tv", (version + versionOffset).ToString())
            }),
            Issuer = "EticaretIdentityServer",
            Audience = RefreshAudience,
            // Süresi geçmiş token için NotBefore da geçmişe çekilir (aksi halde Expires < NotBefore olur)
            NotBefore = expires is { } e && e < now ? e.AddMinutes(-1) : now,
            Expires = expires ?? now.AddDays(1),
            SigningCredentials = new SigningCredentials(
                signingKey ?? JwtKeys.LoadPrivateKey(IdentityApiFactory.PrivateKeyBase64), SecurityAlgorithms.RsaSha256)
        });

        return handler.WriteToken(token);
    }

    private Task<HttpResponseMessage> RefreshAsync(string? refreshToken) => SendWithCookieAsync("/api/auth/refresh", refreshToken);

    private Task<HttpResponseMessage> SendWithCookieAsync(string path, string? refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (refreshToken is not null)
            request.Headers.Add("Cookie", $"{CookieName}={refreshToken}");
        return _client.SendAsync(request);
    }

    private static async Task<string> ReadTokenAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return body?.Token ?? string.Empty;
    }

    private static string? GetRefreshSetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith($"{CookieName}=", StringComparison.Ordinal))
            : null;

    private static string ExtractRefreshToken(HttpResponseMessage response) =>
        GetRefreshSetCookie(response)!.Split(';')[0][(CookieName.Length + 1)..];

    private sealed record TokenResponse(string Token);
}
