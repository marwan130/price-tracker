namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.HasKey(c => c.Code);

        builder.Property(c => c.Code)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(c => c.Symbol)
               .IsRequired()
               .HasMaxLength(10);

        builder.HasMany(c => c.Stores)
               .WithOne(s => s.Currency)
               .HasForeignKey(s => s.CurrencyCode)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.PriceHistories)
               .WithOne(p => p.Currency)
               .HasForeignKey(p => p.CurrencyCode)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Trackings)
               .WithOne(t => t.Currency)
               .HasForeignKey(t => t.CurrencyCode)
               .OnDelete(DeleteBehavior.Restrict);
    }
}