using EticaretMicroservice.Shared.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. RATE LIMITER KONFİGÜRASYONU (Order ve Auth/Basket için ayrı politikalar)
builder.Services.AddRateLimiter(options =>
{
    // Sipariş limiti (10 saniyede maks 10 istek)
    options.AddFixedWindowLimiter("order-policy", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Brute-force ve spam koruması (Identity Login & Basket: 1 dakikada maks 20 istek)
    options.AddFixedWindowLimiter("auth-policy", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\": \"Çok fazla istek gönderildi. Lütfen bir süre sonra tekrar deneyin.\"}",
            cancellationToken);
    };
});

// 2. YARP PROXY
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 🟢 CORS: SignalR ve UI origin kısıtlaması
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000") // Vite & React portları
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // SignalR Hub için zorunlu
    });
});

builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Gateway.Api");

// 3. HEALTH CHECKS UI
builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(15);
    options.MaximumHistoryEntriesPerEndpoint(60);
})
.AddInMemoryStorage();

var app = builder.Build();

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

app.UseCors("CorsPolicy");
app.UseRateLimiter();

app.MapReverseProxy();

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-dashboard";
});

app.Run();