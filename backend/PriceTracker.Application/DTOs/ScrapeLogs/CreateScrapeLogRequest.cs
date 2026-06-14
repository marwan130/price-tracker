namespace PriceTracker.Application.DTOs.ScrapeLogs;

public class CreateScrapeLogRequest
{
    public Guid      StoreId      { get; set; }
    public Guid?     ListingId    { get; set; }
    public string    Status       { get; set; } = string.Empty;
    public string?   ErrorMessage { get; set; }
    public int       ItemsScraped { get; set; }
    public DateTime  StartedAt    { get; set; }
    public DateTime? FinishedAt   { get; set; }
}
