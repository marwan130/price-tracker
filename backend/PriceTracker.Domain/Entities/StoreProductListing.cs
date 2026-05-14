namespace PriceTracker.Domain.Entities;

public class StoreProductListing
{
    public Guid      ListingId     { get; set; }
    public Guid      ProductId     { get; set; }
    public Guid      VariantId     { get; set; }
    public Guid      StoreId       { get; set; }
    public string    ProductUrl    { get; set; } = string.Empty;
    public bool      IsActive      { get; set; } = true;
    public DateTime? LastScrapedAt { get; set; }

    // Navigation properties
    public Product                          Product        { get; set; } = null!;
    public ProductVariant                   Variant        { get; set; } = null!;
    public Store                            Store          { get; set; } = null!;
    public ICollection<PriceHistory>        PriceHistories { get; set; } = new List<PriceHistory>();
    public ICollection<ScrapeLog>           ScrapeLogs     { get; set; } = new List<ScrapeLog>();
    public ICollection<UserProductTracking> Trackings      { get; set; } = new List<UserProductTracking>();
}