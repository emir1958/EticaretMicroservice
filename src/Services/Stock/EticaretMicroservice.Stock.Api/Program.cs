using EticaretMicroservice.Shared.Extensions;
using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Extensions;
using EticaretMicroservice.Stock.Api.Services;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Stock.Api");
builder.Logging.AddSharedLogging(builder.Configuration, "Stock.Api");

builder.Services.AddSharedSwagger();
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddSharedCors();

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

builder.Services.AddMassTransit(x =>
{
    x.AddStockMessaging();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ConfigureSharedRetryAndDeadLetter(context);

        cfg.ConfigureEndpoints(context);
    });
});

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    dbContext.Database.Migrate();
}

app.Run();