namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasKey(s => s.StoreId);

        builder.Property(s => s.StoreId)
               .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(s => s.BaseUrl)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(s => s.Country)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(s => s.CurrencyCode)
               .HasMaxLength(10);

        builder.Property(s => s.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.HasIndex(s => s.Country)
               .HasDatabaseName("idx_stores_country");

        builder.HasIndex(s => s.CurrencyCode)
               .HasDatabaseName("idx_stores_currency_code");

        builder.HasMany(s => s.Listings)
               .WithOne(l => l.Store)
               .HasForeignKey(l => l.StoreId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.ScrapeLogs)
               .WithOne(sl => sl.Store)
               .HasForeignKey(sl => sl.StoreId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}