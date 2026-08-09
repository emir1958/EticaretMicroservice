using EticaretMicroservice.Services.Order.Application.Consumers;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using EticaretMicroservice.Services.Order.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;

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
    x.AddConsumer<StockFailedEventConsumer>();
    // 🔹 EF Core Outbox Kaydı
    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        // Veritabanı sağlayıcısı olarak SQL Server seçiyoruz
        o.UseSqlServer();

        // Bus seviyesinde Outbox kullanımını aktif ediyoruz
        o.UseBusOutbox();

        // (Opsiyonel) Çift mesaj engelleme penceresi
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
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();