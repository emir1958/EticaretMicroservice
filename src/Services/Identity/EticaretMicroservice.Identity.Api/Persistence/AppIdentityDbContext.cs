using EticaretMicroservice.Identity.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Identity.Api.Persistence
{
    public class AppIdentityDbContext : DbContext
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                // 🟢 1. Unique Index (Mükerrer e-posta kayıtlarını DB seviyesinde engeller)
                entity.HasIndex(u => u.Email).IsUnique();

                entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50).HasDefaultValue("User");
            });

            // 🟢 2. Sabit Güvenli Admin Seed Verisi (Parola: Admin123*!)
            var adminId = "b7a2d480-1a22-4826-b841-3965d1d60001";
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*!");

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = adminId,
                Username = "SystemAdmin",
                Email = "admin@eticaret.com",
                PasswordHash = adminPasswordHash,
                Role = "Admin"
            });
        }
    }
}