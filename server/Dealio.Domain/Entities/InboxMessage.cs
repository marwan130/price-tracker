namespace PriceTracker.Domain.Entities;

public class InboxMessage
{
    public Guid MessageId { get; set; }
    public string LogicalKey { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
