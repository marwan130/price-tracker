namespace PriceTracker.Application.DTOs.Listings;

public class ListingResponse
{
    public Guid      ListingId     { get; set; }
    public Guid      ProductId     { get; set; }
    public string    ProductName   { get; set; } = string.Empty;
    public Guid      VariantId     { get; set; }
    public string    VariantSku    { get; set; } = string.Empty;
    public Guid      StoreId       { get; set; }
    public string    StoreName     { get; set; } = string.Empty;
    public string    ProductUrl    { get; set; } = string.Empty;
    public bool      IsActive      { get; set; }
    public DateTime? LastScrapedAt { get; set; }
    public decimal?  CurrentPrice  { get; set; }
    public string?   CurrencyCode  { get; set; }
    public string?   Currency      { get; set; }
}