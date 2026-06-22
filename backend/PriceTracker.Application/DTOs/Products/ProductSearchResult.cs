namespace PriceTracker.Application.DTOs.Products;

public class ProductSearchResult
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string StoreName { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string? VariantInfo { get; set; }
    public decimal? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public bool InStock { get; set; } = true;
}
