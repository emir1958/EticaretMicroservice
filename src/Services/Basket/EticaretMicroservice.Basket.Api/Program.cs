using EticaretMicroservice.Basket.Api.Consumers;
using EticaretMicroservice.Basket.Api.Services;
using EticaretMicroservice.Shared.Extensions;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSharedSwagger();

// 1. OpenTelemetry
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Basket.Api");
builder.Logging.AddOpenTelemetry(loggingOptions =>
{
    loggingOptions.IncludeFormattedMessage = true;
    loggingOptions.IncludeScopes = true;
    loggingOptions.AddOtlpExporter(); // Logları OTLP üzerinden Aspire Dashboard'a gönderir
});
// 2. Redis Bağlantısı
var redisConnString = builder.Configuration["Redis:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnString));

builder.Services.AddScoped<IBasketService, BasketService>();

// 3. Health Checks (Redis + RabbitMQ)
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

builder.Services.AddHealthChecks()
    .AddRedis(
        redisConnectionString: redisConnString,
        name: "Basket-Redis",
        tags: new[] { "cache", "redis" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:5672/",
        name: "Basket-RabbitMQ",
        tags: new[] { "messagebus", "rabbitmq" });

// 4. MassTransit & RabbitMQ (Sipariş tamamlandığında sepeti temizleme tüketimi)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<BasketPaymentCompletedEventConsumer>();

    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        // Ortak Retry & DLQ politikası
        cfg.ConfigureSharedRetryAndDeadLetter(context);

        cfg.ReceiveEndpoint("basket-payment-completed-queue", e =>
        {
            e.ConfigureConsumer<BasketPaymentCompletedEventConsumer>(context);
        });
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 5. JWT Doğrulama (Issuer & Audience korumalı)
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

var app = builder.Build();

// 6. Global Exception Middleware
app.UseCustomExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 7. Health Check Endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();