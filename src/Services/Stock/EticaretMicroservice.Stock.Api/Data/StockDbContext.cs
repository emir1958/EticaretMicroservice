using EticaretMicroservice.Stock.Api.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Data
{
    public class StockDbContext : DbContext
    {
        public StockDbContext(DbContextOptions<StockDbContext> options) : base(options) { }

        public DbSet<ProductStock> ProductStocks { get; set; }
        public DbSet<StockReservation> StockReservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MassTransit tabloları
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            // RowVersion / Optimistic Concurrency
            modelBuilder.Entity<ProductStock>(b =>
            {
                b.Property(x => x.RowVersion).IsRowVersion();

                // Stok satırları her zaman ProductId ile aranır/kilitlenir; index yoksa her UPDATE tabloyu tarar
                // ve farklı ürünlere gelen siparişler birbirini bekler. nvarchar(max) kolona index konamaz.
                b.Property(x => x.ProductId).HasMaxLength(64);
                b.HasIndex(x => x.ProductId).IsUnique();
            });

            modelBuilder.Entity<StockReservation>(b =>
            {
                b.Property(x => x.ProductId).HasMaxLength(64).IsRequired();

                // İş seviyesi idempotency: aynı sipariş aynı ürün için ikinci kez rezervasyon yapamaz
                b.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
            });

            // Seed Veriler
            modelBuilder.Entity<ProductStock>().HasData(
                new ProductStock { Id = 1, ProductId = "66c01f3e7b23d12a98f12345", AvailableStock = 100, ReservedStock = 0 },
                new ProductStock { Id = 2, ProductId = "66c01f3e7b23d12a98f12346", AvailableStock = 50, ReservedStock = 0 }
            );
        }
    }
}