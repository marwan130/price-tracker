namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public sealed class ProductSearchSessionEventConfiguration : IEntityTypeConfiguration<ProductSearchSessionEvent>
{
    public void Configure(EntityTypeBuilder<ProductSearchSessionEvent> builder)
    {
        builder.ToTable("ProductSearchSessionEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.HasIndex(x => new { x.SessionId, x.Sequence }).IsUnique();
    }
}
