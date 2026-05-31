namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class StoreProductListingConfiguration : IEntityTypeConfiguration<StoreProductListing>
{
    public void Configure(EntityTypeBuilder<StoreProductListing> builder)
    {
        builder.HasKey(l => l.ListingId);

        builder.Property(l => l.ListingId)
               .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(l => l.ProductUrl)
               .IsRequired()
               .HasMaxLength(1000);

        builder.Property(l => l.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.HasIndex(l => l.ProductId)
               .HasDatabaseName("idx_listings_product");

        builder.HasIndex(l => l.VariantId)
               .HasDatabaseName("idx_listings_variant");

        builder.HasIndex(l => l.StoreId)
               .HasDatabaseName("idx_listings_store");

        builder.HasIndex(l => l.IsActive)
               .HasDatabaseName("idx_listings_active");

        builder.HasIndex(l => l.LastScrapedAt)
               .IsDescending()
               .HasDatabaseName("idx_listings_scraped_at");

        builder.HasIndex(l => new { l.VariantId, l.StoreId })
               .IsUnique()
               .HasDatabaseName("idx_listings_variant_store");

        builder.HasMany(l => l.PriceHistories)
               .WithOne(p => p.Listing)
               .HasForeignKey(p => p.ListingId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.ScrapeLogs)
               .WithOne(sl => sl.Listing)
               .HasForeignKey(sl => sl.ListingId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(l => l.Trackings)
               .WithOne(t => t.Listing)
               .HasForeignKey(t => t.ListingId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}