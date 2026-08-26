using EticaretMicroservice.Stock.Api.Consumers;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Services;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. DbContext Kaydı
builder.Services.AddDbContext<StockDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 2. Service Kaydı
builder.Services.AddScoped<IStockService, StockService>();

// 🟢 DÜZELTME: HealthCheck Servis Kaydı (SQL Server ve RabbitMQ Kontrolleri)
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "StockDb-SQL",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:5672/",
        name: "Stock-RabbitMQ",
        tags: new[] { "messagebus", "rabbitmq" });

// 3. MassTransit & Consumer Kaydı
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedEventConsumer>();
    x.AddConsumer<PaymentFailedEventConsumer>();
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMqUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitMqPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username(rabbitMqUser);
            h.Password(rabbitMqPass);
        });

        // 🟢 1. Kuyruk: Sipariş Oluşunca Stok Düşme Kuyruğu
        cfg.ReceiveEndpoint("stock-order-created-queue", e =>
        {
            e.ConfigureConsumer<OrderCreatedEventConsumer>(context);
        });

        // 🟢 2. DÜZELTME: Ödeme Başarısız Olunca Stok İade Kuyruğu
        cfg.ReceiveEndpoint("stock-payment-failed-queue", e =>
        {
            e.ConfigureConsumer<PaymentFailedEventConsumer>(context);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();