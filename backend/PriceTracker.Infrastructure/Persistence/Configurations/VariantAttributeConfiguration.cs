namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class VariantAttributeConfiguration : IEntityTypeConfiguration<VariantAttribute>
{
    public void Configure(EntityTypeBuilder<VariantAttribute> builder)
    {
        builder.HasKey(va => new { va.VariantId, va.AttributeValueId });

        builder.HasIndex(va => va.VariantId)
               .HasDatabaseName("idx_variant_attributes_variant");

        builder.HasIndex(va => va.AttributeValueId)
               .HasDatabaseName("idx_variant_attributes_value");
    }
}