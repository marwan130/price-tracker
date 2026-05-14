namespace PriceTracker.Domain.Entities;

public class ProductImage
{
    public long     ImageId   { get; set; }
    public Guid     ProductId { get; set; }
    public string   Url       { get; set; } = string.Empty;
    public bool     IsPrimary { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Product Product { get; set; } = null!;
}