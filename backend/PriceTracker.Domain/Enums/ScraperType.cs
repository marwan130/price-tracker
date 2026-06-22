namespace PriceTracker.Domain.Enums;

/// <summary>
/// Defines the type of scraper strategy to use for a store.
/// </summary>
public enum ScraperType
{
    /// <summary>
    /// Default HTML-based scraper using AngleSharp for static pages.
    /// </summary>
    Html = 0,

    /// <summary>
    /// Playwright-based scraper for JavaScript-heavy sites requiring browser automation.
    /// </summary>
    Playwright = 1,

    /// <summary>
    /// Official API-based scraper for stores that provide product/affiliate APIs.
    /// </summary>
    Api = 2,

    /// <summary>
    /// Store is unsupported due to CAPTCHA or other anti-bot measures.
    /// </summary>
    Unsupported = 3
}
