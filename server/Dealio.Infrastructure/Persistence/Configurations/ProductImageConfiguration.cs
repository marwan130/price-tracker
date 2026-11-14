namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(i => i.ImageId);

        builder.Property(i => i.Url)
               .IsRequired()
               .HasMaxLength(1000);

        builder.Property(i => i.IsPrimary)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(i => i.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.HasIndex(i => i.ProductId)
               .HasDatabaseName("idx_product_images_product");
    }
}