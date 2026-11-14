namespace PriceTracker.Application.DTOs.Products;

public class CreateProductRequest
{
    public string  Name        { get; set; } = string.Empty;
    public string? Brand       { get; set; }
    public long?   CategoryId  { get; set; }
    public string? Description { get; set; }
}