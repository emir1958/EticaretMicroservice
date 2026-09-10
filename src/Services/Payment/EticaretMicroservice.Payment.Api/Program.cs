using EticaretMicroservice.Payment.Api.Consumers;
using EticaretMicroservice.Payment.Api.Data;
using EticaretMicroservice.Payment.Api.Services;
using EticaretMicroservice.Shared.Extensions;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Payment.Api");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IPaymentService, FakePaymentService>();

// 🟢 1. SQL Server & DbContext Kaydı
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1435;Database=PaymentDb;User Id=sa;Password=Password12*!;TrustServerCertificate=True;";

builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    options.UseSqlServer(defaultConnection);
});

// 2. Health Check Kaydı (SQL + RabbitMQ)
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: defaultConnection,
        name: "Payment-SqlServer",
        tags: new[] { "db", "sqlserver" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:5672/",
        name: "Payment-RabbitMQ",
        tags: new[] { "messagebus", "rabbitmq" });

// 🟢 3. MassTransit & Transactional Outbox/Inbox Kaydı
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservedEventConsumer>();

    // Entity Framework Outbox & Inbox entegrasyonu
    x.AddEntityFrameworkOutbox<PaymentDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(10);
    });

    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint("payment-stock-reserved-queue", e =>
        {
            // Mükerrer tüketimi ve kayıpları önleyen Inbox middleware'i
            e.UseEntityFrameworkOutbox<PaymentDbContext>(context);
            e.ConfigureConsumer<StockReservedEventConsumer>(context);
        });
    });
});

var app = builder.Build();

// 🟢 4. Otomatik Migration (Uygulama kalkarken PaymentDb'yi oluşturur)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// 5. Health Check Uç Noktası
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();