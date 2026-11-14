namespace Dealio.Application.DTOs.Products;

public class ProductSearchResult
{
    public Guid? ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string? VariantInfo { get; set; }
    public decimal? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public bool? InStock { get; set; }
    public string MarketCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? DiscoveryProvider { get; set; }
    public bool IsVerified { get; set; }
}
