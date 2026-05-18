namespace PriceTracker.Application.DTOs.Products;

public class ProductFilterRequest
{
    public string? Query      { get; set; }    
    public string? Brand      { get; set; }
    public long?   CategoryId { get; set; }
}