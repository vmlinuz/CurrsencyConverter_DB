using Microsoft.EntityFrameworkCore;

namespace CurrencyConverter_Static.Data
{
    public class CurrencyConverterDbContext : DbContext
    {
        public CurrencyConverterDbContext(DbContextOptions<CurrencyConverterDbContext> options)
            : base(options)
        {
        }

        public DbSet<CurrencyMaster> CurrencyMasters => Set<CurrencyMaster>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CurrencyMaster>(entity =>
            {
                entity.ToTable("Currency_Master");
                entity.HasKey(currency => currency.Id);
                entity.Property(currency => currency.Id).ValueGeneratedOnAdd();
                entity.Property(currency => currency.CurrencyName).HasMaxLength(50);
            });
        }
    }
}
