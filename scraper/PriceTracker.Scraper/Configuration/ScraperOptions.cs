namespace PriceTracker.Scraper.Configuration;

public class ScraperOptions
{
    public int IntervalMinutes        { get; set; } = 60;
    public int RequestTimeoutSeconds  { get; set; } = 30;
    public int DelayBetweenListingsMs { get; set; } = 1000;
    public int ListingPageSize        { get; set; } = 100;
}
