namespace PriceTracker.Domain.Entities;

public class Store
{
    public Guid    StoreId      { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string  BaseUrl      { get; set; } = string.Empty;
    public string  Country      { get; set; } = string.Empty;
    public string? CurrencyCode { get; set; }
    public bool    IsActive     { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Currency?                          Currency   { get; set; }
    public ICollection<StoreProductListing>   Listings   { get; set; } = new List<StoreProductListing>();
    public ICollection<ScrapeLog>             ScrapeLogs { get; set; } = new List<ScrapeLog>();
}