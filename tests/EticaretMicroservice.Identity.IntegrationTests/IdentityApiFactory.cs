using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace EticaretMicroservice.Identity.IntegrationTests;

public class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@tests.local";
    public const string AdminPassword = "Admin-Password-123!";

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_P@ssw0rd123!")
        .Build();

    // Her test çalıştırmasında yeni bir RSA anahtar çifti: yapılandırma değeri PEM'in base64'üdür (üretimdeki biçimle aynı)
    public static readonly string PrivateKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(RSA.Create(2048).ExportPkcs8PrivateKeyPem()));

    public async Task InitializeAsync() => await _dbContainer.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // appsettings.json secret içermediği için testin ihtiyaç duyduğu değerler burada verilir
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("Jwt:PrivateKey", PrivateKeyBase64);
        builder.UseSetting("AdminSeed:Email", AdminEmail);
        builder.UseSetting("AdminSeed:Password", AdminPassword);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.StopAsync();
    }
}
