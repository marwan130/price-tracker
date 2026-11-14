namespace Dealio.Domain.Entities;

public sealed class ProductSearchSession
{
    public Guid SessionId { get; set; }
    public string RequestPayload { get; set; } = string.Empty;
    public Guid? SourceProductId { get; set; }
    public string? MarketCode { get; set; }
    public string Status { get; set; } = "queued";
    public int Attempts { get; set; }
    public int PendingVerifications { get; set; }
    public int VerifiedCandidates { get; set; }
    public string? CompletionPayload { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(1);
    public DateTime? NextAttemptAtUtc { get; set; }
    public ICollection<ProductSearchSessionEvent> Events { get; set; } = new List<ProductSearchSessionEvent>();
}
