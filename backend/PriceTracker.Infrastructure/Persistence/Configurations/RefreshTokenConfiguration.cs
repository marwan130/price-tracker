namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Token)
               .IsRequired()
               .HasMaxLength(512);

        builder.Property(r => r.ExpiresAt)
               .IsRequired();

        builder.Property(r => r.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.HasIndex(r => r.Token)
               .IsUnique()
               .HasDatabaseName("idx_refresh_tokens_token");

        builder.HasIndex(r => r.UserId)
               .HasDatabaseName("idx_refresh_tokens_user_id");

        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}