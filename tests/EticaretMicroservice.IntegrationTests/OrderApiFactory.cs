using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Application.Sagas;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.MsSql;
using Xunit;

namespace EticaretMicroservice.IntegrationTests;

public class OrderApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Testlerde 15 dakikalık ödeme süresini beklememek için saga timeout'u kısaltılır
    public static readonly TimeSpan PaymentTimeout = TimeSpan.FromSeconds(3);

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_P@ssw0rd123!")
        .Build();

    public readonly Mock<ICatalogRepository> CatalogRepoMock = new();

    // Aynı konteynerde, uygulamanın kullandığından bağımsız bir veritabanı (migration testleri için)
    public string GetConnectionString(string database) =>
        new SqlConnectionStringBuilder(_dbContainer.GetConnectionString()) { InitialCatalog = database }.ConnectionString;

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
        builder.UseSetting("OrderSaga:PaymentTimeout", PaymentTimeout.ToString());

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

            // RabbitMQ yerine in-memory transport; Program.cs'teki saga ve outbox kayıtları korunur.
            // Production'daki retry politikasının karşılığı: saga instance'ından önce gelen event'ler ve
            // RowVersion çakışmasını kaybeden mesajlar retry ile yeniden işlenir.
            var registrationsBeforeHarness = services.ToList();

            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<SagaMessageSink>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseMessageRetry(r => r.Intervals(100, 250, 500, 1000, 2000, 3000));
                    cfg.ConfigureEndpoints(context);
                });
            });

            KeepProductionSagaRepository(services, registrationsBeforeHarness);

            // JWT yerine header tabanlı test kimliği
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    // Harness saga repository'lerini in-memory kayıtlarla değiştirir (DI'da son kayıt kazanır).
    // Saga'nın gerçek SQL'de (EF repository, RowVersion) test edilmesi için harness'ın OrderState altyapısına
    // eklediği mükerrer kayıtlar kaldırılır; harness'a özgü (öncesinde olmayan) kayıtlar korunur.
    private static void KeepProductionSagaRepository(IServiceCollection services, IReadOnlyCollection<ServiceDescriptor> registrationsBeforeHarness)
    {
        var productionSagaServiceTypes = registrationsBeforeHarness
            .Where(d => d.ServiceType.GenericTypeArguments.Contains(typeof(OrderState)))
            .Select(d => d.ServiceType)
            .ToHashSet();

        var harnessDuplicates = services
            .Except(registrationsBeforeHarness)
            .Where(d => productionSagaServiceTypes.Contains(d.ServiceType))
            .ToList();

        foreach (var descriptor in harnessDuplicates)
            services.Remove(descriptor);
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}

[CollectionDefinition(Name)]
public class OrderApiCollection : ICollectionFixture<OrderApiFactory>
{
    public const string Name = "OrderApi";
}
