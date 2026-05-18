namespace PriceTracker.Application.DTOs.Notifications;

using PriceTracker.Domain.Enums;

public class NotificationResponse
{
    public long                NotificationId { get; set; }
    public string              ProductName    { get; set; } = string.Empty;
    public string?             VariantSku     { get; set; }
    public string              StoreName      { get; set; } = string.Empty;
    public decimal             TriggeredPrice { get; set; }
    public decimal             TargetPrice    { get; set; }
    public string              CurrencyCode   { get; set; } = string.Empty;
    public NotificationChannel Channel        { get; set; }
    public NotificationStatus  Status         { get; set; }
    public DateTime            SentAt         { get; set; }
}