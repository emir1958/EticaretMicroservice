using EticaretMicroservice.Catalog.Api.Seed;
using EticaretMicroservice.Catalog.Api.Services;
using EticaretMicroservice.Catalog.Api.Settings;
using EticaretMicroservice.Shared.Extensions;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSharedSwagger();

builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Catalog.Api");
builder.Logging.AddOpenTelemetry(loggingOptions =>
{
    loggingOptions.IncludeFormattedMessage = true;
    loggingOptions.IncludeScopes = true;
    loggingOptions.AddOtlpExporter(); // Logları OTLP üzerinden Aspire Dashboard'a gönderir
});
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

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ConfigureSharedRetryAndDeadLetter(context);
    });
});

builder.Services.AddSharedCors();

builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.AddSingleton<IDatabaseSettings>(sp =>
    sp.GetRequiredService<IOptions<DatabaseSettings>>().Value);

// MongoClient thread-safe'tir ve uygulama boyunca tek örnek olarak kullanılması önerilir (kendi bağlantı havuzunu yönetir)
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IDatabaseSettings>().ConnectionString));

builder.Services.AddScoped<IProductService, ProductService>();

if (builder.Configuration.GetValue<bool>("Seed:SampleData"))
{
    builder.Services.AddHostedService<SampleCatalogSeeder>();
}

var app = builder.Build();

app.UseCustomExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsExtensions.PolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();