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

            // Admin kullanıcısı burada HasData ile değil, Program.cs'te yapılandırmadan okunan şifreyle seed edilir.
        }
    }
}