namespace PriceTracker.Application.DTOs.Products;

public class ProductFilterRequest
{
    public string?  Query      { get; set; }    
    public string?  Brand      { get; set; }
    public long?    CategoryId { get; set; }
    public Guid?    StoreId    { get; set; }
    public decimal? MinPrice   { get; set; }
    public decimal? MaxPrice   { get; set; }
    public string?  SortBy     { get; set; }
}