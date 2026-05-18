namespace PriceTracker.Application.DTOs.Products;

using PriceTracker.Application.DTOs.Variants;

public class ProductResponse
{
    public Guid     ProductId   { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string?  Brand       { get; set; }
    public string?  Category    { get; set; }
    public string?  Description { get; set; }
    public string?  PrimaryImage { get; set; }
    public DateTime CreatedAt   { get; set; }
    public IEnumerable<VariantResponse> Variants { get; set; } = new List<VariantResponse>();
}