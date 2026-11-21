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

    public async Task InitializeAsync() => await _dbContainer.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // appsettings.json secret içermediği için testin ihtiyaç duyduğu değerler burada verilir
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("Jwt:SecretKey", "identity-tests-only-signing-key-0123456789");
        builder.UseSetting("AdminSeed:Email", AdminEmail);
        builder.UseSetting("AdminSeed:Password", AdminPassword);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.StopAsync();
    }
}
