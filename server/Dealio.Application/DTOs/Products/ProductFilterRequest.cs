namespace Dealio.Application.DTOs.Products;

public class ProductFilterRequest
{
    public string?  Query      { get; set; }    
    public string?  Brand      { get; set; }
    public long?    CategoryId { get; set; }
    public Guid?    StoreId    { get; set; }
    public decimal? MinPrice   { get; set; }
    public decimal? MaxPrice   { get; set; }
    public string?  SortBy     { get; set; }
    public string?  RandomSeed { get; set; }
    public string?  Market     { get; set; }
    public int       Page       { get; set; }

    public bool HasValidPriceRange()
        => MinPrice is not < 0 && MaxPrice is not < 0
           && Page >= 0
           && !(MinPrice.HasValue && MaxPrice.HasValue && MinPrice.Value > MaxPrice.Value);
}
