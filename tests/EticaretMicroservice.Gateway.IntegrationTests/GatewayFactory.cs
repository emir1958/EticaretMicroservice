using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EticaretMicroservice.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Forwarder;

namespace EticaretMicroservice.Gateway.IntegrationTests;

public class GatewayFactory : WebApplicationFactory<Program>
{
    // Token'ı "Identity" yerine testler imzalar; gateway yalnızca bu anahtarın genel kısmını bilir (üretimdeki gibi)
    private static readonly RSA IdentityRsa = RSA.Create(2048);
    private static readonly string PrivateKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(IdentityRsa.ExportPkcs8PrivateKeyPem()));
    private static readonly string PublicKeyPem = IdentityRsa.ExportSubjectPublicKeyInfoPem();
    private static readonly RsaSecurityKey SigningKey = JwtKeys.LoadPrivateKey(PrivateKeyBase64);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:PublicKey", Convert.ToBase64String(Encoding.UTF8.GetBytes(PublicKeyPem)));

        // Arka uç servisler yok: limiter'dan geçen istek, anında 200 dönen sahte bir istemciye yönlenir.
        // (Gerçek ağ bağlantısı denemek Windows'ta istek başına ~2 sn sürer.) Testler yalnızca "429 oldu mu" sorusuna bakar.
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IForwarderHttpClientFactory, StubForwarderHttpClientFactory>();
            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
        });
    }

    private static int _nextClient;

    // TestServer'da RemoteIpAddress boştur ve tüm istekler aynı "IP" kovasına düşerdi; her istemciye ayrı IP verilir
    public HttpClient CreateClientWithOwnIp()
    {
        var n = Interlocked.Increment(ref _nextClient);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, $"10.0.{n / 256}.{n % 256}");
        return client;
    }

    private sealed class TestClientIpStartupFilter : IStartupFilter
    {
        public const string Header = "X-Test-Client-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(Header, out var ip))
                    context.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
                return nextMiddleware(context);
            });
            next(app);
        };
    }

    private sealed class StubForwarderHttpClientFactory : IForwarderHttpClientFactory
    {
        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context) => new(new StubHandler());
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    // Gateway'in kabul etmesi gereken, Identity gibi imzalanmış geçerli token
    public static string CreateToken(string userId) =>
        WriteToken(userId, new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256));

    // Saldırgan 1: kendi RSA anahtarıyla imzalar (Identity'nin anahtarıyla değil)
    public static string CreateTokenSignedByAnotherKey(string userId) =>
        WriteToken(userId, new SigningCredentials(new RsaSecurityKey(RSA.Create(2048)), SecurityAlgorithms.RsaSha256));

    // Saldırgan 2 (algoritma karıştırma): herkese açık genel anahtarı HMAC sırrı gibi kullanıp HS256 token üretir
    public static string CreateHs256TokenSignedWithPublicKey(string userId) =>
        WriteToken(userId, new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PublicKeyPem)),
            SecurityAlgorithms.HmacSha256));

    private static string WriteToken(string userId, SigningCredentials credentials)
    {
        var token = new JwtSecurityToken(
            issuer: "EticaretIdentityServer",
            audience: "EticaretGateway",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static async Task<HttpStatusCode> SendAsync(HttpClient client, HttpMethod method, string path, string? bearerToken = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (bearerToken is not null)
            request.Headers.Authorization = new("Bearer", bearerToken);

        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}
