namespace PriceTracker.Scraper.Configuration;

public class ScraperOptions
{
    public int IntervalMinutes        { get; set; } = 60;
    public int RequestTimeoutSeconds  { get; set; } = 30;
    public int DelayBetweenListingsMs { get; set; } = 1000;
    public int ListingPageSize        { get; set; } = 100;
    public string? SearchQuery        { get; set; }
    public int? CategoryId            { get; set; }
    public Guid? StoreId              { get; set; }
    public decimal? MinPrice          { get; set; }
    public decimal? MaxPrice          { get; set; }
    public string? CurrencyCode       { get; set; }
}