namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class AttributeValueConfiguration : IEntityTypeConfiguration<AttributeValue>
{
    public void Configure(EntityTypeBuilder<AttributeValue> builder)
    {
        builder.HasKey(a => a.AttributeValueId);

        builder.Property(a => a.Value)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(a => a.AttributeTypeId)
               .HasDatabaseName("idx_attribute_values_type");

        builder.HasIndex(a => new { a.AttributeTypeId, a.Value })
               .IsUnique()
               .HasDatabaseName("idx_attribute_values_type_value");

        builder.HasMany(a => a.VariantAttributes)
               .WithOne(va => va.AttributeValue)
               .HasForeignKey(va => va.AttributeValueId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}