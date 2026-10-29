using MassTransit;
using Microsoft.EntityFrameworkCore;
using EticaretMicroservice.Payment.Api.Models;

namespace EticaretMicroservice.Payment.Api.Data;


public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentRecord> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Aynı sipariş için ikinci bir çekim kaydı eklenemez
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.Property(x => x.TotalPrice).HasPrecision(18, 2);
        });
    }
}