namespace PriceTracker.Scraper.Scraping;

/// <summary>
/// Factory for selecting the appropriate store scraper based on store configuration.
/// </summary>
public class StoreScraperFactory
{
    private readonly IEnumerable<IStoreScraper> _scrapers;
    private readonly ILogger<StoreScraperFactory> _logger;

    public StoreScraperFactory(
        IEnumerable<IStoreScraper> scrapers,
        ILogger<StoreScraperFactory> logger)
    {
        _scrapers = scrapers;
        _logger = logger;
    }

    /// <summary>
    /// Gets the appropriate scraper for the given store type.
    /// </summary>
    /// <param name="scraperType">The scraper type.</param>
    /// <returns>The scraper instance.</returns>
    public IStoreScraper GetScraper(string scraperType)
    {
        var scraper = _scrapers.FirstOrDefault(s => s.ScraperType.Equals(scraperType, StringComparison.OrdinalIgnoreCase));
        if (scraper == null)
        {
            throw new NotSupportedException($"Scraper type '{scraperType}' is not supported");
        }
        return scraper;
    }

    /// <summary>
    /// Gets the appropriate scraper for the given URL.
    /// Uses the first scraper that can handle the URL.
    /// </summary>
    /// <param name="url">The URL to scrape.</param>
    /// <returns>The scraper instance.</returns>
    public IStoreScraper GetScraperForUrl(string url)
    {
        var scraper = _scrapers.FirstOrDefault(s => s.CanHandle(url));
        if (scraper != null)
        {
            _logger.LogDebug("Selected {ScraperType} for URL: {Url}", scraper.ScraperType, url);
            return scraper;
        }

        // Default to HTML scraper as fallback
        var fallback = _scrapers.FirstOrDefault(s => s.ScraperType.Equals("html", StringComparison.OrdinalIgnoreCase));
        if (fallback == null)
        {
            throw new InvalidOperationException("HTML scraper is not registered");
        }
        return fallback;
    }
}
