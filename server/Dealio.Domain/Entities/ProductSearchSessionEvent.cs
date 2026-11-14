namespace Dealio.Domain.Entities;

public sealed class ProductSearchSessionEvent
{
    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public int Sequence { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public ProductSearchSession Session { get; set; } = null!;
}
