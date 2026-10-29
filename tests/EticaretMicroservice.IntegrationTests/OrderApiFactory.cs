using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.MsSql;
using Xunit;

namespace EticaretMicroservice.IntegrationTests;

public class OrderApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_P@ssw0rd123!")
        .Build();

    public readonly Mock<ICatalogRepository> CatalogRepoMock = new();

    public async Task InitializeAsync()
    {
        // 1. SQL Server konteynerini başlat
        await _dbContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // appsettings.json secret içermediği için testin ihtiyaç duyduğu değerler burada verilir
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("Jwt:SecretKey", "integration-tests-only-signing-key-0123456789");

        builder.ConfigureTestServices(services =>
        {
            // Mevcut SQL DbContext ayarını kaldır
            services.RemoveAll(typeof(DbContextOptions<OrderDbContext>));

            // Konteynerin ürettiği dinamik bağlantı dizesi ile DB'yi bağla
            services.AddDbContext<OrderDbContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString(), sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
                });
            });

            // Catalog HttpClient bağımlılığını mock ile ez
            services.RemoveAll(typeof(ICatalogRepository));
            services.AddSingleton(CatalogRepoMock.Object);
        });
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}