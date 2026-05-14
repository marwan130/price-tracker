namespace PriceTracker.Domain.Entities;

public class Product
{
    public Guid     ProductId   { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string?  Brand       { get; set; }
    public long?    CategoryId  { get; set; }
    public string?  Description { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Category?                        Category  { get; set; }
    public ICollection<ProductImage>        Images    { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant>      Variants  { get; set; } = new List<ProductVariant>();
    public ICollection<StoreProductListing> Listings  { get; set; } = new List<StoreProductListing>();
    public ICollection<UserProductTracking> Trackings { get; set; } = new List<UserProductTracking>();
}