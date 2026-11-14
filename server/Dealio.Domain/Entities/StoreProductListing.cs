namespace Dealio.Domain.Entities;

public class StoreProductListing
{
    public Guid      ListingId     { get; set; }
    public Guid      ProductId     { get; set; }
    public Guid      StoreId       { get; set; }
    public string    ProductUrl    { get; set; } = string.Empty;
    public bool      IsActive      { get; set; } = true;
    public bool      HasVerifiedPrice { get; set; } = true;
    public bool      IsExcluded    { get; set; }
    public DateTime? LastScrapedAt { get; set; }
    public DateTime? LastRefreshQueuedAtUtc { get; set; }
    public Product                          Product        { get; set; } = null!;
    public Store                            Store          { get; set; } = null!;
    public ICollection<PriceHistory>        PriceHistories { get; set; } = new List<PriceHistory>();
    public ICollection<ScrapeLog>           ScrapeLogs     { get; set; } = new List<ScrapeLog>();
    public ICollection<UserProductTracking> Trackings      { get; set; } = new List<UserProductTracking>();
}
