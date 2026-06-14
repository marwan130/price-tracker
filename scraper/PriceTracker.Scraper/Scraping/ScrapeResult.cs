namespace PriceTracker.Scraper.Scraping;

public class ScrapeResult
{
    public decimal Price        { get; init; }
    public string  CurrencyCode { get; init; } = "USD";
}
