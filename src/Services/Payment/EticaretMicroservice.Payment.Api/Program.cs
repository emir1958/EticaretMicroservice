using EticaretMicroservice.Payment.Api.Consumers;
using EticaretMicroservice.Payment.Api.Data;
using EticaretMicroservice.Payment.Api.Services;
using EticaretMicroservice.Shared.Commands;
using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Shared.Extensions;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Payment.Api");
builder.Logging.AddSharedLogging(builder.Configuration, "Payment.Api");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IPaymentService, FakePaymentService>();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection yapılandırması eksik!");

builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    options.UseSqlServer(defaultConnection);
});

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

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservedEventConsumer>();
    x.AddConsumer<RefundPaymentCommandConsumer>();

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

        cfg.ConfigureSharedRetryAndDeadLetter(context);

        cfg.ReceiveEndpoint("payment-stock-reserved-queue", e =>
        {
            e.UseEntityFrameworkOutbox<PaymentDbContext>(context);
            e.ConfigureConsumer<StockReservedEventConsumer>(context);

            e.Bind<StockReservedEvent>();
        });

        // Order saga'sının iade komutu (iptal edilmiş siparişe gelen ödeme)
        cfg.ReceiveEndpoint("payment-refund-queue", e =>
        {
            e.UseEntityFrameworkOutbox<PaymentDbContext>(context);
            e.ConfigureConsumer<RefundPaymentCommandConsumer>(context);
            e.Bind<RefundPaymentCommand>();
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

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

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();