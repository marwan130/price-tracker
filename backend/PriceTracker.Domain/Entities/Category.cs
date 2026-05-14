namespace PriceTracker.Domain.Entities;

public class Category
{
    public long   CategoryId { get; set; }
    public string Name       { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}