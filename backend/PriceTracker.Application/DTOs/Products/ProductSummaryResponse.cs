namespace PriceTracker.Application.DTOs.Products;

public class ProductSummaryResponse
{
    public Guid     ProductId    { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string?  Brand        { get; set; }
    public string?  Category     { get; set; }
    public string?  PrimaryImage { get; set; }
    public decimal? LowestPrice  { get; set; }
    public string?  Currency     { get; set; }
    public int      StoreCount   { get; set; }
}