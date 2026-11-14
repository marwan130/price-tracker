namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public sealed class ProductSearchSessionConfiguration : IEntityTypeConfiguration<ProductSearchSession>
{
    public void Configure(EntityTypeBuilder<ProductSearchSession> builder)
    {
        builder.ToTable("ProductSearchSessions");
        builder.HasKey(x => x.SessionId);
        builder.Property(x => x.RequestPayload).HasMaxLength(4096).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.Property(x => x.MarketCode).HasMaxLength(8);
        builder.Property(x => x.CompletionPayload);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc, x.CreatedAtUtc });
        builder.HasIndex(x => x.ExpiresAtUtc);
        builder.HasIndex(x => new { x.SourceProductId, x.MarketCode, x.CreatedAtUtc });
        builder.HasMany(x => x.Events).WithOne(x => x.Session).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
