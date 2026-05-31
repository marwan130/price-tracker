namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.VariantId);

        builder.Property(v => v.VariantId)
               .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(v => v.Sku)
               .HasMaxLength(200);

        builder.Property(v => v.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(v => v.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.HasIndex(v => v.ProductId)
               .HasDatabaseName("idx_product_variants_product");

        builder.HasIndex(v => v.Sku)
               .HasDatabaseName("idx_product_variants_sku");

        builder.HasIndex(v => v.IsActive)
               .HasDatabaseName("idx_product_variants_active");

        builder.HasIndex(v => new { v.ProductId, v.Sku })
               .IsUnique()
               .HasDatabaseName("idx_product_variants_product_sku")
               .HasFilter("sku IS NOT NULL");

        builder.HasMany(v => v.VariantAttributes)
               .WithOne(va => va.Variant)
               .HasForeignKey(va => va.VariantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Listings)
               .WithOne(l => l.Variant)
               .HasForeignKey(l => l.VariantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Trackings)
               .WithOne(t => t.Variant)
               .HasForeignKey(t => t.VariantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}