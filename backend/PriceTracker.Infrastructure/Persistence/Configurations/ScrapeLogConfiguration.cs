namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

public class ScrapeLogConfiguration : IEntityTypeConfiguration<ScrapeLog>
{
    public void Configure(EntityTypeBuilder<ScrapeLog> builder)
    {
        builder.HasKey(s => s.LogId);

        builder.Property(s => s.Status)
               .IsRequired()
               .HasConversion<string>();

        builder.Property(s => s.ErrorMessage)
               .HasColumnType("text");

        builder.Property(s => s.ItemsScraped)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(s => s.StartedAt)
               .IsRequired();

        builder.HasIndex(s => s.StoreId)
               .HasDatabaseName("idx_scrape_logs_store");

        builder.HasIndex(s => s.ListingId)
               .HasDatabaseName("idx_scrape_logs_listing");

        builder.HasIndex(s => s.Status)
               .HasDatabaseName("idx_scrape_logs_status");

        builder.HasIndex(s => s.StartedAt)
               .IsDescending()
               .HasDatabaseName("idx_scrape_logs_started");
    }
}