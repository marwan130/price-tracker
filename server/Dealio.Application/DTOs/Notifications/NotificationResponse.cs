namespace Dealio.Application.DTOs.Notifications;

using Dealio.Domain.Enums;

public class NotificationResponse
{
    public long                NotificationId { get; set; }
    public Guid?               ProductId      { get; set; }
    public Guid                TrackingId     { get; set; }
    public Guid?               ListingId      { get; set; }
    public NotificationEventType EventType     { get; set; }
    public string              ProductName    { get; set; } = string.Empty;
    public string              StoreName      { get; set; } = string.Empty;
    public decimal             TriggeredPrice { get; set; }
    public decimal?            ConvertedTriggeredPrice { get; set; }
    public decimal?            ConversionRate { get; set; }
    public DateTime?           RateUpdatedAtUtc { get; set; }
    public string              TriggeredCurrencyCode { get; set; } = string.Empty;
    public string              TargetCurrencyCode { get; set; } = string.Empty;
    public decimal             TargetPrice    { get; set; }
    public string              CurrencyCode   { get; set; } = string.Empty;
    public NotificationChannel Channel        { get; set; }
    public NotificationStatus  Status         { get; set; }
    public bool                IsRead         { get; set; }
    public DateTime            SentAt         { get; set; }
}
