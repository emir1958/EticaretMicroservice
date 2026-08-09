using EticaretMicroservice.Services.Order.Domain.Entities;
using MassTransit; // 👈 Bu using tanımının eklendiğinden emin olun
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.EntityFrameworkCoreIntegration;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Infrastructure.Persistence
{
    public class OrderDbContext : DbContext
    {
        public const string DEFAULT_SCHEMA = "ordering";

        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Domain.Entities.Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(DEFAULT_SCHEMA);

            // Order Entity Konfigürasyonu
            modelBuilder.Entity<Domain.Entities.Order>(b =>
            {
                b.ToTable("Orders");
                b.HasKey(o => o.Id);

                // Value Object (Address) konfigürasyonu - Single table (OwnsOne)
                b.OwnsOne(o => o.Address, a =>
                {
                    a.Property(p => p.City).HasColumnName("City").HasMaxLength(50);
                    a.Property(p => p.District).HasColumnName("District").HasMaxLength(50);
                    a.Property(p => p.Street).HasColumnName("Street").HasMaxLength(100);
                    a.Property(p => p.ZipCode).HasColumnName("ZipCode").HasMaxLength(10);
                    a.Property(p => p.Line).HasColumnName("Line").HasMaxLength(250);
                });

                // OrderItems ilişkisi (Backing Field için)
                b.HasMany(o => o.OrderItems)
                 .WithOne()
                 .HasForeignKey("OrderId")
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // OrderItem Entity Konfigürasyonu
            modelBuilder.Entity<OrderItem>(b =>
            {
                b.ToTable("OrderItems");
                b.HasKey(i => i.Id);
                b.Property(i => i.Price).HasPrecision(18, 2);
            });

            base.OnModelCreating(modelBuilder);

            // 🔹 MassTransit 8.x Transactional Outbox Entity Mapping
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }
    }
}