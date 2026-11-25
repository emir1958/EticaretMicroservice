using EticaretMicroservice.Gateway.Extensions;
using EticaretMicroservice.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Kullanıcı/IP başına kovalar (ayrıntı RateLimitingExtensions.cs içinde)
builder.Services.AddGatewayRateLimiting();

// Limiter kullanıcıyı partition anahtarı yapar; bu yüzden token burada da doğrulanır (servislerle aynı Jwt ayarları)
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSharedCors();

builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Gateway.Api");

builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(15);
    options.MaximumHistoryEntriesPerEndpoint(60);
})
.AddInMemoryStorage();

var app = builder.Build();
app.UseCors(CorsExtensions.PolicyName);

// Geçerli bir X-Correlation-ID gelmediyse üretilir; servislere iletilen istek ve yanıt aynı id'yi taşır
app.Use(async (context, next) =>
{
    const string correlationIdHeaderKey = "X-Correlation-ID";

    if (!context.Request.Headers.TryGetValue(correlationIdHeaderKey, out var correlationId) ||
        string.IsNullOrWhiteSpace(correlationId) ||
        !Guid.TryParse(correlationId, out _))
    {
        correlationId = Guid.NewGuid().ToString();
        context.Request.Headers[correlationIdHeaderKey] = correlationId;
    }

    context.Response.Headers[correlationIdHeaderKey] = correlationId;
    await next();
});

// Kimlik, limiter'dan önce çözülmeli: basket/order policy'leri partition anahtarını context.User'dan alır
app.UseAuthentication();
app.UseRateLimiter();

app.MapReverseProxy();

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-dashboard";
});

app.Run();

// Gateway testleri WebApplicationFactory ile bu uygulamayı başlatır
public partial class Program { }