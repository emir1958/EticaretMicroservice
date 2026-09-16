using EticaretMicroservice.Catalog.Api.Services;
using EticaretMicroservice.Catalog.Api.Settings;
using EticaretMicroservice.Shared.Extensions;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSharedSwagger();

// 1. OpenTelemetry Kaydı (Aspire Dashboard izlemesi için)
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Catalog.Api");
builder.Logging.AddOpenTelemetry(loggingOptions =>
{
    loggingOptions.IncludeFormattedMessage = true;
    loggingOptions.IncludeScopes = true;
    loggingOptions.AddOtlpExporter(); // Logları OTLP üzerinden Aspire Dashboard'a gönderir
});
// 2. Health Checks (MongoDB + RabbitMQ)
var mongoConn = builder.Configuration["DatabaseSettings:ConnectionString"] ?? "mongodb://localhost:27017";
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

builder.Services.AddHealthChecks()
    .AddMongoDb(
        mongodbConnectionString: mongoConn,
        name: "Catalog-MongoDB",
        tags: new[] { "db", "mongo" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:5672/",
        name: "Catalog-RabbitMQ",
        tags: new[] { "messagebus", "rabbitmq" });

// 3. MassTransit & RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        // Ortak retry politikası
        cfg.ConfigureSharedRetryAndDeadLetter(context);
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

// 4. JWT Yetkilendirme (Issuer + Audience doğrulamalı extension)
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.AddSingleton<IDatabaseSettings>(sp =>
    sp.GetRequiredService<IOptions<DatabaseSettings>>().Value);

builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// 5. Global Exception Middleware (En üstte olmalı)
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

// 6. Health Check Endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();