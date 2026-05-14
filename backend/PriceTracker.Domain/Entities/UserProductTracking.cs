namespace PriceTracker.Domain.Entities;

public class UserProductTracking
{
    public Guid     TrackingId   { get; set; }
    public Guid     UserId       { get; set; }
    public Guid     ProductId    { get; set; }
    public Guid?    VariantId    { get; set; }
    public Guid?    ListingId    { get; set; }
    public double   TargetPrice  { get; set; }
    public string   CurrencyCode { get; set; } = string.Empty;
    public bool     IsActive     { get; set; } = true;
    public bool     NotifyEmail  { get; set; } = true;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User                      User          { get; set; } = null!;
    public Product                   Product       { get; set; } = null!;
    public ProductVariant?           Variant       { get; set; } = null!;
    public StoreProductListing?      Listing       { get; set; } = null!;
    public Currency                  Currency      { get; set; } = null!;
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}