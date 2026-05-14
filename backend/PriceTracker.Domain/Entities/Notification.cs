namespace PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

public class Notification
{
    public long                 NotificationId  { get; set; }
    public Guid                 TrackingId      { get; set; }
    public Guid                 UserId          { get; set; }
    public long                 PriceHistoryId  { get; set; }
    public decimal              TriggeredPrice  { get; set; }
    public decimal              TargetPrice     { get; set; }
    public DateTime             SentAt          { get; set; } = DateTime.UtcNow;
    public NotificationChannel  Channel         { get; set; } = NotificationChannel.Email;
    public NotificationStatus   Status          { get; set; } = NotificationStatus.Pending;

    // Navigation properties
    public UserProductTracking Tracking     { get; set; } = null!;
    public User                User         { get; set; } = null!;
    public PriceHistory        PriceHistory { get; set; } = null!;
}