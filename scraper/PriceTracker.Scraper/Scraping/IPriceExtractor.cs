namespace PriceTracker.Scraper.Scraping;

public interface IPriceExtractor
{
    Task<ScrapeResult?> ExtractAsync(string html, string? fallbackCurrencyCode = null, CancellationToken ct = default);
}
