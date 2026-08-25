using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
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
// 1. YARP ve CORS Servislerini Ekle
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
// 🔹 Health Checks UI Dashboard Servis Kaydı
builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(15); // 15 saniyede bir servisleri kontrol et
    options.MaximumHistoryEntriesPerEndpoint(60);
})
.AddInMemoryStorage(); // Dashboard verilerini belpekte tutar

var app = builder.Build();

// 2. Middleware sıralaması
app.UseCors("AllowAll");
app.UseRateLimiter();
// Gelen istekleri appsettings.json'daki kurallara göre arkadaki servislere pasla
app.MapReverseProxy();
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-dashboard"; // Dashboard erişim adresi
});
app.Run();