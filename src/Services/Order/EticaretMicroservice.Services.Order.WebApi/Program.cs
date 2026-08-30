using EticaretMicroservice.Services.Order.Application.Consumers;
using EticaretMicroservice.Services.Order.Application.Hubs;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using EticaretMicroservice.Services.Order.Infrastructure.Repositories;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using EticaretMicroservice.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. DbContext Konfigürasyonu (SQL Server)
builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), configure =>
    {
        configure.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
    });
});

// 2. Repository Injection
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 3. MediatR Registrasyonu
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IOrderRepository).Assembly));

// 4. MassTransit, RabbitMQ & Transactional Outbox Konfigürasyonu
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentCompletedEventConsumer>();
    x.AddConsumer<PaymentFailedEventConsumer>();
    x.AddConsumer<StockFailedEventConsumer>();

    // 🔹 EF Core Outbox Kaydı
    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(5);
    });

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

        cfg.ReceiveEndpoint("order-stock-failed-queue", e =>
        {
            e.ConfigureConsumer<StockFailedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("order-payment-failed-queue", e =>
        {
            e.ConfigureConsumer<PaymentFailedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("order-payment-completed-queue", e =>
        {
            e.ConfigureConsumer<PaymentCompletedEventConsumer>(context);
        });
    });
});

builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Order.WebApi");

// 🟢 CORS Servis Kaydı Eklendi (App.UseCors için gerekli)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSharedSwagger();
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddSignalR();

// 🔹 Health Check Servis Kaydı
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "OrderDb-SQL",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:5672/",
        name: "Order-RabbitMQ",
        tags: new[] { "messagebus", "rabbitmq" });

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
app.MapHub<OrderHub>("/orderhub");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.Migrate();
}

app.Run();