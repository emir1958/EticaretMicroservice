using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Payment.Api.Data;

public class PaymentRecord
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string PaymentToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

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