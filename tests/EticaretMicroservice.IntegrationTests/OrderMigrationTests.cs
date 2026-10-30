using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EticaretMicroservice.IntegrationTests;

// Veri taşıyan migration'lar boş veritabanında test edilemez: önce eski şema kurulur ve doldurulur,
// sonra yeni migration uygulanır ve mevcut satırlara ne olduğu doğrulanır.
[Collection(OrderApiCollection.Name)]
public class OrderMigrationTests
{
    private const string MigrationBeforeCorrelationId = "20260929173244_initaltablecolumn";

    private readonly OrderApiFactory _factory;

    public OrderMigrationTests(OrderApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AddOrderCorrelationId_BackfillsDistinctIdsForExistingOrders_AndRemovesDefault()
    {
        var connectionString = _factory.GetConnectionString($"OrderDb_Migration_{Guid.NewGuid():N}");
        await using var context = new OrderDbContext(new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(connectionString, o => o.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName))
            .Options);
        var migrator = context.GetService<IMigrator>();

        // Arrange: CorrelationId kolonu yokken oluşturulmuş "eski" siparişler
        await migrator.MigrateAsync(MigrationBeforeCorrelationId);
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO ordering.Orders (BuyerId, CreatedDate, OrderStatus, City, District, Street, ZipCode, Line)
            VALUES (N'legacy-1', SYSUTCDATETIME(), 2, N'İstanbul', N'Kadıköy', N'Moda', N'34710', N'No:1'),
                   (N'legacy-2', SYSUTCDATETIME(), 3, N'İstanbul', N'Kadıköy', N'Moda', N'34710', N'No:2'),
                   (N'legacy-3', SYSUTCDATETIME(), 1, N'İstanbul', N'Kadıköy', N'Moda', N'34710', N'No:3')
            """);

        // Act
        await migrator.MigrateAsync();

        // Assert: her eski sipariş ayrı ve boş olmayan bir CorrelationId almış olmalı
        var correlationIds = await context.Database
            .SqlQueryRaw<Guid>("SELECT CorrelationId AS Value FROM ordering.Orders")
            .ToListAsync();

        correlationIds.Should().HaveCount(3)
            .And.OnlyHaveUniqueItems()
            .And.NotContain(Guid.Empty);

        // Assert: backfill için eklenen NEWID() default'u kaldırılmış olmalı
        var defaultConstraintCount = await context.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS Value
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID('ordering.Orders') AND c.name = 'CorrelationId'
            """).SingleAsync();

        defaultConstraintCount.Should().Be(0);
    }
}
