using EticaretMicroservice.Stock.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace EticaretMicroservice.Stock.Api.Data
{
    public class StockDbContext : DbContext
    {
        public StockDbContext(DbContextOptions options) : base(options) { }

        public DbSet<ProductStock> ProductStocks { get; set; }
        // Opsiyonel: Veritabanı ilk oluştuğunda test için örnek veriler ekleyelim
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductStock>().HasData(
    new ProductStock { Id = 1, ProductId = "prod-1", AvailableStock = 100 },
    new ProductStock { Id = 2, ProductId = "prod-2", AvailableStock = 50 }
);
            base.OnModelCreating(modelBuilder);
        }
    }
}
