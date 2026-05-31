namespace PriceTracker.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.NotificationId);

        builder.Property(n => n.TriggeredPrice)
               .IsRequired()
               .HasColumnType("numeric(12,2)");

        builder.Property(n => n.TargetPrice)
               .IsRequired()
               .HasColumnType("numeric(12,2)");

        builder.Property(n => n.SentAt)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.Property(n => n.Channel)
               .IsRequired()
               .HasConversion<string>()
               .HasDefaultValue(NotificationChannel.Email);

        builder.Property(n => n.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasDefaultValue(NotificationStatus.Pending);

        builder.HasIndex(n => n.UserId)
               .HasDatabaseName("idx_notifications_user");

        builder.HasIndex(n => n.TrackingId)
               .HasDatabaseName("idx_notifications_tracking");

        builder.HasIndex(n => n.SentAt)
               .IsDescending()
               .HasDatabaseName("idx_notifications_sent_at");

        builder.HasIndex(n => n.Status)
               .HasDatabaseName("idx_notifications_status");
    }
}