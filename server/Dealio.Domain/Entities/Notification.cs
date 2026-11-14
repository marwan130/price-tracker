namespace Dealio.Domain.Entities;
using Dealio.Domain.Enums;

public class Notification
{
    public long                 NotificationId  { get; set; }
    public Guid                 TrackingId      { get; set; }
    public Guid                 UserId          { get; set; }
    public long                 PriceHistoryId  { get; set; }
    public decimal              TriggeredPrice  { get; set; }
    public decimal?             ConvertedTriggeredPrice { get; set; }
    public decimal?             ConversionRate { get; set; }
    public DateTime?            RateUpdatedAtUtc { get; set; }
    public decimal              TargetPrice     { get; set; }
    public DateTime             SentAt          { get; set; } = DateTime.UtcNow;
    public NotificationChannel  Channel         { get; set; } = NotificationChannel.Email;
    public NotificationEventType EventType      { get; set; } = NotificationEventType.TargetReached;
    public NotificationStatus   Status          { get; set; } = NotificationStatus.Pending;
    public bool                 IsRead          { get; set; }
    public UserProductTracking Tracking     { get; set; } = null!;
    public User                User         { get; set; } = null!;
    public PriceHistory        PriceHistory { get; set; } = null!;
}
