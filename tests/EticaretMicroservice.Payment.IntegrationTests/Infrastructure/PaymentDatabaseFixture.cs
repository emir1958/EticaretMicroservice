using EticaretMicroservice.Payment.Api.Data;
using EticaretMicroservice.Payment.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace EticaretMicroservice.Payment.IntegrationTests.Infrastructure;

public sealed class PaymentDatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "PaymentDb_IntegrationTests"
        }.ConnectionString;

        // Production ile aynı şema: gerçek migration'lar uygulanır
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public PaymentDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PaymentDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task<PaymentRecord> SeedPaymentAsync(PaymentStatus status, decimal totalPrice = 100m)
    {
        var payment = new PaymentRecord
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = Random.Shared.Next(1_000_000, int.MaxValue),
            BuyerId = "it-buyer",
            TotalPrice = totalPrice,
            PaymentToken = "tok_it_success",
            Status = status
        };

        await using var context = CreateDbContext();
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        return payment;
    }

    public async Task<PaymentStatus> GetStatusAsync(int orderId)
    {
        await using var context = CreateDbContext();
        return await context.Payments.AsNoTracking().Where(p => p.OrderId == orderId).Select(p => p.Status).SingleAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PaymentDatabaseCollection : ICollectionFixture<PaymentDatabaseFixture>
{
    public const string Name = "PaymentDatabase";
}
