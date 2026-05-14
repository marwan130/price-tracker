namespace PriceTracker.Domain.Entities;

public class ProductVariant
{
    public Guid    VariantId { get; set; }
    public Guid    ProductId { get; set; }
    public string? Sku       { get; set; }
    public bool    IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Product                          Product            { get; set; } = null!;
    public ICollection<VariantAttribute>    VariantAttributes  { get; set; } = new List<VariantAttribute>();
    public ICollection<StoreProductListing> Listings           { get; set; } = new List<StoreProductListing>();
    public ICollection<UserProductTracking> Trackings          { get; set; } = new List<UserProductTracking>();
}