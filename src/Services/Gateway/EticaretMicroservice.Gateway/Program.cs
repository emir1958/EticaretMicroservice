using EticaretMicroservice.Gateway.Extensions;
using EticaretMicroservice.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. RATE LIMITER: kullanıcı/IP başına kovalar (ayrıntı RateLimitingExtensions.cs içinde)
builder.Services.AddGatewayRateLimiting();

// Limiter kullanıcıyı partition anahtarı yapar; bu yüzden token burada da doğrulanır (servislerle aynı Jwt ayarları)
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// 2. YARP PROXY
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSharedCors();

builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Gateway.Api");

// 3. HEALTH CHECKS UI
builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(15);
    options.MaximumHistoryEntriesPerEndpoint(60);
})
.AddInMemoryStorage();

var app = builder.Build();
app.UseCors(CorsExtensions.PolicyName);

// 🟢 4. CORRELATION ID MIDDLEWARE (YARP'a taşınacak temiz Guid zinciri)
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