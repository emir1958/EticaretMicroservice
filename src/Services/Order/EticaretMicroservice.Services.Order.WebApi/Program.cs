using EticaretMicroservice.Services.Order.Application.Behaviors;
using EticaretMicroservice.Services.Order.Application.Consumers;
using EticaretMicroservice.Services.Order.Application.Hubs;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Application.Validators;
using EticaretMicroservice.Services.Order.Infrastructure.BackgroundServices;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using EticaretMicroservice.Services.Order.Infrastructure.Repositories;
using EticaretMicroservice.Shared.Extensions;
using FluentValidation;
using HealthChecks.UI.Client;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;

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

// 3. MediatR & FluentValidation Pipeline Behavior Kaydı
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(IOrderRepository).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommandValidator).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

// 4. MassTransit, RabbitMQ & Transactional Outbox
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentCompletedEventConsumer>();
    x.AddConsumer<PaymentFailedEventConsumer>();
    x.AddConsumer<StockFailedEventConsumer>();

    // Outbox: Event'leri önce SQL'e atomik kaydeder
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

        cfg.ConfigureSharedRetryAndDeadLetter(context);

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

builder.Services.AddOptions<MassTransitHostOptions>()
    .Configure(options =>
    {
        options.WaitUntilStarted = true;
    });

// 🟢 5. OpenTelemetry Tracing & Aspire Dashboard Logging
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Order.WebApi");
builder.Logging.AddSharedLogging(builder.Configuration, "Order.WebApi");

// 6. CORS Yapılandırması (SignalR Credentials Desteği ile)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSharedSwagger();

// 7. JWT Authentication + SignalR WebSockets Query Token Desteği
builder.Services.AddSharedJwtAuthentication(builder.Configuration, signalRHubPath: "/orderhub");

builder.Services.AddSignalR();
builder.Services.AddHostedService<OrderTimeoutWorker>();

// 8. Health Check Servis Kaydı
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";
var formattedRabbitHost = rabbitHost.Contains(":") ? rabbitHost : $"{rabbitHost}:5672";

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "OrderDb-SQL",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{formattedRabbitHost}/",
        name: "Order-RabbitMQ",
        tags: new[] { "messagebus", "rabbitmq" });

// 9. Catalog HttpClient + Polly Resilience Handler
var catalogUrl = builder.Configuration["ServiceUrls:Catalog"] ?? "http://catalog.api:8080";

builder.Services.AddHttpClient<ICatalogRepository, CatalogRepository>(client =>
{
    client.BaseAddress = new Uri(catalogUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(7);
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Retry.Delay = TimeSpan.FromMilliseconds(300);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.FailureRatio = 0.6;
    options.CircuitBreaker.MinimumThroughput = 10;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

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