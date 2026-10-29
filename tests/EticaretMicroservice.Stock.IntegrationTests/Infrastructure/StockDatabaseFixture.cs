using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace EticaretMicroservice.Stock.IntegrationTests.Infrastructure;

// Tüm test sınıfları tek bir SQL Server konteynerini paylaşır (konteyner açılışı ~10-20 sn sürer).
// Testler birbirini etkilemesin diye her test kendi ProductId / OrderId değerlerini üretir.
public sealed class StockDatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "StockDb_IntegrationTests"
        }.ConnectionString;

        // Production ile aynı şema: gerçek migration'lar uygulanır
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public StockDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<StockDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task<string> SeedProductAsync(int availableStock)
    {
        var productId = $"it-{Guid.NewGuid():N}";

        await using var context = CreateDbContext();
        context.ProductStocks.Add(new ProductStock { ProductId = productId, AvailableStock = availableStock });
        await context.SaveChangesAsync();

        return productId;
    }

    public async Task<ProductStock> GetStockAsync(string productId)
    {
        await using var context = CreateDbContext();
        return await context.ProductStocks.AsNoTracking().SingleAsync(s => s.ProductId == productId);
    }
}

[CollectionDefinition(Name)]
public sealed class StockDatabaseCollection : ICollectionFixture<StockDatabaseFixture>
{
    public const string Name = "StockDatabase";
}
