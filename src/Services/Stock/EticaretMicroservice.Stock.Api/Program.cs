using EticaretMicroservice.Shared.Extensions;
using EticaretMicroservice.Stock.Api.Consumers;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Services;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 🟢 OpenTelemetry: Tracing ve Logging tek merkezden bağlanır
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://aspire-dashboard:4317";
var serviceName = "Stock.Api";

// 🟢 OpenTelemetry: Tracing ve Logging tek merkezden bağlanır
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Stock.Api");
builder.Logging.AddSharedLogging(builder.Configuration, "Stock.Api");

builder.Services.AddSharedSwagger();
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// 🟢 CORS: İsim standardizasyonu
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<StockDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IStockService, StockService>();

var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";
var formattedRabbitHost = rabbitHost.Contains(":") ? rabbitHost : $"{rabbitHost}:5672";

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "StockDb-SQL",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{formattedRabbitHost}/",
        name: "Stock-RabbitMQ",
        tags: new[] { "messagebus", "rabbitmq" });

// 🟢 MassTransit: Consumer'ların kuyruğa bağlanması
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedEventConsumer>();
    x.AddConsumer<PaymentFailedEventConsumer>();
    x.AddConsumer<ProductCreatedEventConsumer>();
    x.AddConsumer<StockUpdatedEventConsumer>();
    x.AddConsumer<StockPaymentCompletedEventConsumer>();
    x.AddEntityFrameworkOutbox<StockDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ConfigureSharedRetryAndDeadLetter(context);

        // 🟢 KRİTİK: Kayıtlı tüm consumer'lar için kuyrukları otomatik açıp bağlar
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🟢 DÜZELTİLDİ: "AllowAll" yerine tanımlanan "AllowFrontend" politikası çağrılır
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    dbContext.Database.Migrate();
}

app.Run();