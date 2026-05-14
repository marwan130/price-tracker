namespace PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

public class ScrapeLog
{
    public long          LogId         { get; set; }
    public Guid          StoreId       { get; set; }
    public Guid?         ListingId     { get; set; }
    public ScrapeStatus  Status        { get; set; }
    public string?       ErrorMessage  { get; set; }
    public int           ItemsScraped  { get; set; } = 0;
    public DateTime      StartedAt     { get; set; }
    public DateTime?     FinishedAt    { get; set; }

    // Navigation properties
    public Store                Store   { get; set; } = null!;
    public StoreProductListing? Listing { get; set; }
}