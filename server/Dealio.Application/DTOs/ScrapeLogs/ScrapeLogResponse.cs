namespace PriceTracker.Application.DTOs.ScrapeLogs;

public class ScrapeLogResponse
{
    public long LogId { get; set; }
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public Guid? ListingId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int ItemsScraped { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
