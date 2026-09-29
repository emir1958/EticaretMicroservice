using EticaretMicroservice.Identity.Api.Persistence;
using EticaretMicroservice.Identity.Api.Services;
using EticaretMicroservice.Shared.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSharedSwagger();

// 1. OpenTelemetry
builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Identity.Api");
builder.Logging.AddOpenTelemetry(loggingOptions =>
{
    loggingOptions.IncludeFormattedMessage = true;
    loggingOptions.IncludeScopes = true;
    loggingOptions.AddOtlpExporter(); // Logları OTLP üzerinden Aspire Dashboard'a gönderir
});
// 2. DbContext (SQL Server)
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1435;Database=IdentityDb;User Id=sa;Password=Password12*!;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
{
    options.UseSqlServer(defaultConnection);
});

// 3. Identity Servis Entegrasyonu (Custom BCrypt Auth)
builder.Services.AddScoped<IIdentityService, IdentityService>();

// 4. SQL Server Health Check
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: defaultConnection,
        name: "Identity-SqlServer",
        tags: new[] { "db", "sqlserver" });

// 5. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 6. Global Exception Middleware
app.UseCustomExceptionHandler();

// Otomatik Migration
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
    dbContext.Database.Migrate();
    var adminEmail = "admin@eticaret.com";
    if (!dbContext.Users.Any(u => u.Email == adminEmail))
    {
        dbContext.Users.Add(new EticaretMicroservice.Identity.Api.Models.User
        {
            Id = "b7a2d480-1a22-4826-b841-3965d1d60001",
            Username = "SystemAdmin",
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("1"),
            Role = "Admin"
        });
        dbContext.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();

// 7. Health Check Endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();