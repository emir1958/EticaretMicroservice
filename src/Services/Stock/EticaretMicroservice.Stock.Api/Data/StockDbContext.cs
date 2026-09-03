using EticaretMicroservice.Stock.Api.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Data
{
    public class StockDbContext : DbContext
    {
        public StockDbContext(DbContextOptions<StockDbContext> options) : base(options) { }

        public DbSet<ProcessedMessage> ProcessedMessages { get; set; }
        public DbSet<ProductStock> ProductStocks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🟢 MassTransit Transactional Outbox & Inbox tablolarını topluca tanımlar
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            // RowVersion / Optimistic Concurrency
            modelBuilder.Entity<ProductStock>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            // Seed Veriler
            modelBuilder.Entity<ProductStock>().HasData(
                new ProductStock { Id = 1, ProductId = "66c01f3e7b23d12a98f12345", AvailableStock = 100, ReservedStock = 0 },
                new ProductStock { Id = 2, ProductId = "66c01f3e7b23d12a98f12346", AvailableStock = 50, ReservedStock = 0 }
            );
        }
    }
}