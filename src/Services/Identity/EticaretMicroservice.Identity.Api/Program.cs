using EticaretMicroservice.Identity.Api.Dtos;
using EticaretMicroservice.Identity.Api.Persistence;
using EticaretMicroservice.Identity.Api.Services;
using EticaretMicroservice.Identity.Api.Validators;
using EticaretMicroservice.Shared.Extensions;
using EticaretMicroservice.Shared.SampleData;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSharedSwagger();

builder.Services.AddSharedOpenTelemetry(builder.Configuration, "Identity.Api");
builder.Logging.AddOpenTelemetry(loggingOptions =>
{
    loggingOptions.IncludeFormattedMessage = true;
    loggingOptions.IncludeScopes = true;
    loggingOptions.AddOtlpExporter(); // Logları OTLP üzerinden Aspire Dashboard'a gönderir
});
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection yapılandırması eksik!");

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
{
    options.UseSqlServer(defaultConnection);
});

builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();
builder.Services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: defaultConnection,
        name: "Identity-SqlServer",
        tags: new[] { "db", "sqlserver" });

builder.Services.AddSharedCors();

// İmza anahtarı (RS256): özel anahtar yalnızca Identity'dedir. Diğer servisler genel kısmı /.well-known/jwks.json üzerinden alır.
var privateKeyPem = builder.Configuration["Jwt:PrivateKey"]
    ?? throw new InvalidOperationException("Jwt:PrivateKey yapılandırması eksik! Üretmek için: openssl genrsa 2048 | base64 | tr -d '\\n'");
var signingKey = JwtKeys.LoadPrivateKey(privateKeyPem);
builder.Services.AddSingleton(signingKey);

// Kullanıcı listesi (api/users) yalnızca Admin rolüne açık: Identity kendi ürettiği token'ı kendi genel anahtarıyla doğrular
builder.Services.AddSharedJwtAuthentication(builder.Configuration, signingKey: JwtKeys.ToPublicKey(signingKey));

var app = builder.Build();

app.UseCustomExceptionHandler();

// Otomatik Migration
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
    dbContext.Database.Migrate();

    // Admin seed: şifre koda gömülmez, yapılandırmadan (AdminSeed:Password) okunur
    var adminEmail = (builder.Configuration["AdminSeed:Email"] ?? "admin@eticaret.com").Trim().ToLowerInvariant();
    var adminPassword = builder.Configuration["AdminSeed:Password"];

    if (string.IsNullOrWhiteSpace(adminPassword))
    {
        app.Logger.LogWarning("AdminSeed:Password yapılandırılmamış, admin kullanıcısı oluşturulmadı.");
    }
    else if (!dbContext.Users.Any(u => u.Email == adminEmail))
    {
        dbContext.Users.Add(new EticaretMicroservice.Identity.Api.Models.User
        {
            Id = "b7a2d480-1a22-4826-b841-3965d1d60001",
            Username = "SystemAdmin",
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = "Admin"
        });
        dbContext.SaveChanges();
    }

    // Örnek müşteriler (yalnızca Seed:SampleData açıkken): eksik olanlar eklenir, mevcut kullanıcılara dokunulmaz.
    // Şifre rastgele üretilip saklanmadığı için bu hesaplarla giriş yapılamaz.
    if (builder.Configuration.GetValue<bool>("Seed:SampleData"))
    {
        var existingIds = dbContext.Users.Select(u => u.Id).ToHashSet();
        var missing = SampleCustomers.Customers.Where(c => !existingIds.Contains(c.Id)).ToList();

        foreach (var customer in missing)
        {
            dbContext.Users.Add(new EticaretMicroservice.Identity.Api.Models.User
            {
                Id = customer.Id,
                Username = customer.Username,
                Email = customer.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                Role = "User"
            });
        }

        if (missing.Count > 0)
            dbContext.SaveChanges();
    }
}

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

// Integration testlerin WebApplicationFactory ile bu uygulamayı başlatabilmesi için
public partial class Program { }