namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.ProductId);

        builder.Property(p => p.ProductId)
               .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(300);

        builder.Property(p => p.Brand)
               .HasMaxLength(150);

        builder.Property(p => p.Description)
               .HasColumnType("text");

        builder.Property(p => p.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.HasIndex(p => p.Brand)
               .HasDatabaseName("idx_products_brand");

        builder.HasIndex(p => p.CategoryId)
               .HasDatabaseName("idx_products_category");

        builder.HasIndex(p => p.Name)
               .HasDatabaseName("idx_products_name_fts")
               .HasMethod("GIN")
               .HasOperators("gin_trgm_ops");

        builder.HasMany(p => p.Images)
               .WithOne(i => i.Product)
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Variants)
               .WithOne(v => v.Product)
               .HasForeignKey(v => v.ProductId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Listings)
               .WithOne(l => l.Product)
               .HasForeignKey(l => l.ProductId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Trackings)
               .WithOne(t => t.Product)
               .HasForeignKey(t => t.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}