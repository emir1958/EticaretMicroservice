using EticaretMicroservice.Shared.Extensions;
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
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Stock.Api");
builder.Services.AddSharedSwagger();
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// 🟢 CORS Servis Kaydı
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 1. DbContext Kaydı
builder.Services.AddDbContext<StockDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 2. Service Kaydı
builder.Services.AddScoped<IStockService, StockService>();

// 3. HealthCheck Servis Kaydı
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

// 4. MassTransit & Consumer Kaydı
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedEventConsumer>();
    x.AddConsumer<PaymentFailedEventConsumer>();
    x.AddConsumer<ProductCreatedEventConsumer>(); 

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

        cfg.ConfigureSharedRetry(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// 🟢 Otomatik Migration (StockDb veritabanı yoksa oluşturulur ve tablolar güncellenir)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    dbContext.Database.Migrate();
}

app.Run();