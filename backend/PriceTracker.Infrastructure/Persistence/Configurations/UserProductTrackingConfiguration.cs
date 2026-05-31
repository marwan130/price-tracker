namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class UserProductTrackingConfiguration : IEntityTypeConfiguration<UserProductTracking>
{
    public void Configure(EntityTypeBuilder<UserProductTracking> builder)
    {
        builder.HasKey(t => t.TrackingId);

        builder.Property(t => t.TrackingId)
               .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.TargetPrice)
               .IsRequired()
               .HasColumnType("numeric(12,2)");

        builder.Property(t => t.CurrencyCode)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(t => t.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(t => t.NotifyEmail)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.HasIndex(t => t.UserId)
               .HasDatabaseName("idx_tracking_user");

        builder.HasIndex(t => t.ProductId)
               .HasDatabaseName("idx_tracking_product");

        builder.HasIndex(t => t.VariantId)
               .HasDatabaseName("idx_tracking_variant");

        builder.HasIndex(t => t.ListingId)
               .HasDatabaseName("idx_tracking_listing");

        builder.HasIndex(t => t.IsActive)
               .HasDatabaseName("idx_tracking_active");

        builder.HasIndex(t => new { t.UserId, t.ProductId, t.VariantId, t.ListingId })
               .IsUnique()
               .HasDatabaseName("idx_tracking_unique");

        builder.HasMany(t => t.Notifications)
               .WithOne(n => n.Tracking)
               .HasForeignKey(n => n.TrackingId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}