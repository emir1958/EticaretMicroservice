using System.Net;
using FluentAssertions;
using static EticaretMicroservice.Gateway.IntegrationTests.GatewayFactory;

namespace EticaretMicroservice.Gateway.IntegrationTests;

public class RateLimitingTests : IClassFixture<GatewayFactory>
{
    private readonly GatewayFactory _factory;
    private readonly HttpClient _client;

    public RateLimitingTests(GatewayFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientWithOwnIp();
    }

    [Fact]
    public async Task Login_IsLimitedAfter20RequestsPerMinute()
    {
        var statuses = await SendManyAsync(HttpMethod.Post, "/api/auth/login", count: 21);

        statuses.Take(20).Should().NotContain(HttpStatusCode.TooManyRequests);
        statuses[20].Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_LimitIsPerIp_OneClientCannotLockOutOthers()
    {
        var innocent = _factory.CreateClientWithOwnIp();

        var attackerStatuses = await SendManyAsync(HttpMethod.Post, "/api/auth/login", count: 21);
        var innocentStatus = await SendAsync(innocent, HttpMethod.Post, "/api/auth/login");

        attackerStatuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
        innocentStatus.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Basket_AllowsFarMoreThan20RequestsPerMinute()
    {
        // Eski ortak "auth-policy" sepeti de dakikada 20 istekle sınırlıyordu
        var token = CreateToken(Guid.NewGuid().ToString());

        var statuses = await SendManyAsync(HttpMethod.Get, "/api/basket", count: 100, token);

        statuses.Should().NotContain(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Basket_LimitIsPerUser_OneUserCannotBlockAnother()
    {
        var heavyUser = CreateToken(Guid.NewGuid().ToString());
        var otherUser = CreateToken(Guid.NewGuid().ToString());

        var heavyStatuses = await SendManyAsync(HttpMethod.Get, "/api/basket", count: 121, heavyUser);
        var otherStatus = await SendAsync(_client, HttpMethod.Get, "/api/basket", otherUser);

        heavyStatuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
        otherStatus.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Basket_UsageDoesNotConsumeLoginLimit()
    {
        var user = CreateToken(Guid.NewGuid().ToString());
        await SendManyAsync(HttpMethod.Get, "/api/basket", count: 60, user);

        var login = await SendAsync(_client, HttpMethod.Post, "/api/auth/login");

        login.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Basket_ForgedTokens_ShareOneBucketInsteadOfEscapingTheLimit()
    {
        // Doğrulanmayan token kullanıcı sayılırsa her istekte farklı sahte token ile limit aşılırdı
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 121; i++)
            statuses.Add(await SendAsync(_client, HttpMethod.Get, "/api/basket", $"forged-token-{i}"));

        statuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task OrderReads_AreNotSubjectToTheOrderCreationLimit()
    {
        // Admin panelindeki bir sayfa birkaç okuma isteğini PARALEL atar; sipariş oluşturma limiti (10 istek / 10 sn, kuyruk 2)
        // okumaya uygulansaydı fazlası 429 alırdı. (Sıralı göndermek burada işe yaramaz: fazla istek tek tek kuyruğa girip geçerdi.)
        var token = CreateToken(Guid.NewGuid().ToString());

        var statuses = await Task.WhenAll(
            Enumerable.Range(0, 40).Select(_ => SendAsync(_client, HttpMethod.Get, "/api/orders/stats/summary", token)));

        statuses.Should().NotContain(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task OrderCreation_IsStillLimitedPerUser()
    {
        var token = CreateToken(Guid.NewGuid().ToString());

        // 10 istek hemen geçer, 2'si sıraya girer (pencere yenilenince geçer), fazlası 429 alır
        var statuses = await Task.WhenAll(
            Enumerable.Range(0, 14).Select(_ => SendAsync(_client, HttpMethod.Post, "/api/orders", token)));

        statuses.Count(s => s == HttpStatusCode.TooManyRequests).Should().BeGreaterThan(0);
        statuses.Count(s => s != HttpStatusCode.TooManyRequests).Should().BeGreaterThanOrEqualTo(10);
    }

    [Theory]
    [InlineData("baska-rsa-anahtari")]
    [InlineData("hs256-genel-anahtar-ile")]
    public async Task Basket_TokensNotSignedByIdentity_AreTreatedAsAnonymous(string attack)
    {
        // Doğrulanamayan token kullanıcı sayılsaydı her istekte farklı kullanıcıyla limit aşılırdı;
        // hepsi tek (IP) kovasına düşmeli ve 121. istek 429 almalı
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 121; i++)
        {
            var userId = $"{attack}-{i}";
            var forged = attack == "baska-rsa-anahtari"
                ? CreateTokenSignedByAnotherKey(userId)
                : CreateHs256TokenSignedWithPublicKey(userId);
            statuses.Add(await SendAsync(_client, HttpMethod.Get, "/api/basket", forged));
        }

        statuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
    }

    private async Task<List<HttpStatusCode>> SendManyAsync(HttpMethod method, string path, int count, string? token = null)
    {
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < count; i++)
            statuses.Add(await SendAsync(_client, method, path, token));
        return statuses;
    }
}
