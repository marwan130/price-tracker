namespace PriceTracker.Application.DTOs.Products;


public class ProductResponse
{
    public Guid     ProductId   { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string?  Brand       { get; set; }
    public string?  Category    { get; set; }
    public string?  Description { get; set; }
    public string?  PrimaryImage { get; set; }
    public DateTime CreatedAt   { get; set; }
    public decimal? LowestPrice  { get; set; }
    public string?  Currency     { get; set; }
    public string?  CurrencyCode { get; set; }
}
