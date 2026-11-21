using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace EticaretMicroservice.Identity.IntegrationTests;

public class AuthApiTests : IClassFixture<IdentityApiFactory>
{
    private const string Password = "Abcdef12";

    private readonly HttpClient _client;

    public AuthApiTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterThenLogin_ReturnsTokenWithUserRoleAndUserId()
    {
        var email = NewEmail();

        (await RegisterAsync(email)).StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await LoginAsync(email, Password);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "User");
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == email);
        jwt.Subject.Should().NotBeNullOrEmpty();
        jwt.Issuer.Should().Be("EticaretIdentityServer");
        jwt.Audiences.Should().Contain("EticaretGateway");
    }

    [Fact]
    public async Task Register_WithRoleFieldInBody_DoesNotGrantAdmin()
    {
        var email = NewEmail();

        // Dışarıdan rol belirtilse bile (mass assignment) hesap User olarak açılmalı
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { username = "u", email, password = Password, role = "Admin" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(await LoginAsync(email, Password));
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "User");
        jwt.Claims.Should().NotContain(c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public async Task Register_SameEmailInDifferentCase_IsRejected()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await RegisterAsync(email.ToUpperInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownEmail_Returns401()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        (await PostLoginAsync(email, "Wrong-Password-1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(NewEmail(), Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Users_RequiresAdminRole()
    {
        var userToken = await RegisterAndLoginAsync();
        var adminToken = await LoginAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword);

        (await GetUsersAsync(token: null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetUsersAsync(userToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var asAdmin = await GetUsersAsync(adminToken);
        asAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
        (await asAdmin.Content.ReadAsStringAsync()).Should().Contain(IdentityApiFactory.AdminEmail);
    }

    [Fact]
    public async Task Login_WithInvalidBody_Returns400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"errors\"");
    }

    private static string NewEmail() => $"user{Guid.NewGuid():N}@tests.local";

    private Task<HttpResponseMessage> RegisterAsync(string email) =>
        _client.PostAsJsonAsync("/api/auth/register", new { username = "tester", email, password = Password });

    private Task<HttpResponseMessage> PostLoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await PostLoginAsync(email, password);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        return await LoginAsync(email, Password);
    }

    private Task<HttpResponseMessage> GetUsersAsync(string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    private sealed record LoginResponse(string Token);
}
