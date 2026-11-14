namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(x => x.MessageId);
        builder.Property(x => x.MessageType).HasMaxLength(160).IsRequired();
        builder.Property(x => x.LogicalKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.LogicalKey).IsUnique();
        builder.HasIndex(x => x.ProcessedAtUtc);
    }
}
