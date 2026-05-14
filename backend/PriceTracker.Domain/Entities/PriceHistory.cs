namespace PriceTracker.Domain.Entities;

public class PriceHistory
{
    public long     Id           { get; set; }
    public Guid     ListingId    { get; set; }
    public decimal  Price        { get; set; }
    public string   CurrencyCode { get; set; } = string.Empty;
    public decimal? PriceInUsd   { get; set; }
    public DateTime RecordedAt   { get; set; }
    public DateTime ScrapedAt    { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public StoreProductListing          Listing       { get; set; } = null!;
    public Currency                     Currency      { get; set; } = null!;
    public ICollection<Notification>    Notifications { get; set; } = new List<Notification>();
}