using EticaretMicroservice.Shared.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. RATE LIMITER KONFİGÜRASYONU
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed-policy", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // 🔹 Limiti aşan isteklerde dönülecek özel yanıt
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\": \"Çok fazla istek gönderildi. Lütfen bekleyip tekrar deneyin.\"}",
            cancellationToken);
    };
});

// 2. YARP VE CORS SERVİSLERİ
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Gateway.Api");
// 3. HEALTH CHECKS UI DASHBOARD
builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(15);
    options.MaximumHistoryEntriesPerEndpoint(60);
})
.AddInMemoryStorage();

var app = builder.Build();

// 4. MIDDLEWARE PIPELINE

// 🟢 CORRELATION ID MIDDLEWARE (En başa ekliyoruz ki tüm istekler ID alabilsin)
app.Use(async (context, next) =>
{
    const string correlationIdHeaderKey = "X-Correlation-ID";

    // 1. İstekte header yoksa veya boşsa yeni bir Guid üret
    if (!context.Request.Headers.TryGetValue(correlationIdHeaderKey, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
        context.Request.Headers[correlationIdHeaderKey] = correlationId;
    }

    // 2. Yanıt (Response) header'ına da ekleyelim ki istemci takip edebilsin
    context.Response.Headers[correlationIdHeaderKey] = correlationId;

    await next();
});

app.UseCors("AllowAll");
app.UseRateLimiter();

// 5. ENDPOINT MAPPING
app.MapReverseProxy();

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-dashboard";
});

app.Run();