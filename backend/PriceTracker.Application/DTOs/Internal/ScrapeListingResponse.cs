namespace PriceTracker.Application.DTOs.Internal;

public class ScrapeListingResponse
{
    public Guid     ListingId    { get; set; }
    public Guid     StoreId      { get; set; }
    public string   StoreName    { get; set; } = string.Empty;
    public string   ProductUrl   { get; set; } = string.Empty;
    public string?  CurrencyCode { get; set; }
}
