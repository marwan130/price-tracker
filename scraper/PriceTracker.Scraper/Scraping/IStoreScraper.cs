namespace PriceTracker.Scraper.Scraping;

/// <summary>
/// Strategy interface for store-specific scraping implementations.
/// Different stores may require different scraping approaches (HTML, Playwright, API, etc.).
/// </summary>
public interface IStoreScraper
{
    /// <summary>
    /// Gets the scraper type identifier.
    /// </summary>
    string ScraperType { get; }

    /// <summary>
    /// Scrapes the product page and extracts price information.
    /// </summary>
    /// <param name="url">The product URL to scrape.</param>
    /// <param name="currencyCode">The expected currency code (fallback).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Scrape result with price and currency, or null if extraction failed.</returns>
    Task<ScrapeResult?> ScrapeAsync(string url, string? currencyCode = null, CancellationToken ct = default);

    /// <summary>
    /// Determines if this scraper can handle the given URL.
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <returns>True if this scraper can handle the URL.</returns>
    bool CanHandle(string url);
}
