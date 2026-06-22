namespace PriceTracker.Scraper.Api;

public class ScrapeListingDto
{
    public Guid     ListingId    { get; set; }
    public Guid     StoreId      { get; set; }
    public string   StoreName    { get; set; } = string.Empty;
    public string   ProductUrl   { get; set; } = string.Empty;
    public string?  CurrencyCode { get; set; }
    public string   ScraperType  { get; set; } = "Html";
}
