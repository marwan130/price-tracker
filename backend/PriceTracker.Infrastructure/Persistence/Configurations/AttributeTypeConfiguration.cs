namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class AttributeTypeConfiguration : IEntityTypeConfiguration<AttributeType>
{
    public void Configure(EntityTypeBuilder<AttributeType> builder)
    {
        builder.HasKey(a => a.AttributeTypeId);

        builder.Property(a => a.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(a => a.Name)
               .IsUnique()
               .HasDatabaseName("idx_attribute_types_name");

        builder.HasMany(a => a.Values)
               .WithOne(v => v.AttributeType)
               .HasForeignKey(v => v.AttributeTypeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}