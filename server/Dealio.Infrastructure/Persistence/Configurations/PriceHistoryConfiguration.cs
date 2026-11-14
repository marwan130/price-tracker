namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Price)
               .IsRequired()
               .HasColumnType("numeric(12,2)");

        builder.Property(p => p.CurrencyCode)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(p => p.PriceInUsd)
               .HasColumnType("numeric(12,2)");

        builder.Property(p => p.RecordedAt)
               .IsRequired();

        builder.Property(p => p.ScrapedAt)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.HasIndex(p => new { p.ListingId, p.RecordedAt })
               .IsDescending(false, true)
               .HasDatabaseName("idx_price_history_listing_time");

        builder.HasIndex(p => new { p.ListingId, p.ScrapedAt })
               .IsDescending(false, true)
               .HasDatabaseName("idx_price_history_listing_scrape");

        builder.HasIndex(p => p.RecordedAt)
               .IsDescending()
               .HasDatabaseName("idx_price_history_recorded_at");

        builder.HasIndex(p => p.CurrencyCode)
               .HasDatabaseName("idx_price_history_currency");

        builder.HasAlternateKey(p => new { p.ListingId, p.RecordedAt });

        builder.HasMany(p => p.Notifications)
               .WithOne(n => n.PriceHistory)
               .HasForeignKey(n => n.PriceHistoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}