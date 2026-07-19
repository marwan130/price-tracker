namespace PriceTracker.Application.Scrapers.Implementations;

using System.Globalization;
using System.Text.RegularExpressions;
using PriceTracker.Application.DTOs.Products;
using Microsoft.Extensions.Logging;

public sealed class ProductDetailScraper : IProductDetailScraper
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ProductDetailScraper> _logger;

    public ProductDetailScraper(IHttpClientFactory factory, ILogger<ProductDetailScraper> logger)
    {
        _factory = factory;
        _logger  = logger;
    }

    public async Task<ProductSearchResult?> ScrapeAsync(string url, CancellationToken ct = default)
    {
        try
        {
            var html = await ScraperHelpers.FetchHtmlAsync(_factory, url, ct, timeoutSecs: 15);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var doc        = ScraperHelpers.HtmlParser.ParseDocument(html);
            var structured = ScraperHelpers.ExtractStructuredProduct(
                doc.QuerySelectorAll("script[type='application/ld+json']").Select(s => s.TextContent));

            var name = structured?.Name
                ?? doc.QuerySelector("h1")?.TextContent?.Trim()
                ?? doc.QuerySelector("meta[property='og:title']")?.GetAttribute("content")?.Trim();

            var description = structured?.Description
                ?? doc.QuerySelector("meta[property='og:description']")?.GetAttribute("content")?.Trim()
                ?? doc.QuerySelector("meta[name='description']")?.GetAttribute("content")?.Trim()
                ?? string.Empty;

            var imageUrl = structured?.ImageUrl
                ?? doc.QuerySelector("meta[property='og:image']")?.GetAttribute("content")?.Trim()
                ?? doc.QuerySelector("img")?.GetAttribute("src")?.Trim()
                ?? string.Empty;

            var price = structured?.Price ?? 0;
            if (price <= 0)
            {
                var raw =
                    doc.QuerySelector("meta[property='product:price:amount']")?.GetAttribute("content")
                 ?? doc.QuerySelector("meta[property='og:price:amount']")?.GetAttribute("content")
                 ?? doc.QuerySelector("[data-price]")?.GetAttribute("data-price")
                 ?? doc.QuerySelector("[itemprop='price']")?.GetAttribute("content")
                 ?? doc.QuerySelector("[itemprop='price']")?.TextContent
                 ?? doc.QuerySelector(".a-price .a-offscreen")?.TextContent
                 ?? doc.QuerySelector(".price")?.TextContent
                 ?? doc.QuerySelector(".prc")?.TextContent;

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var m = Regex.Match(raw, @"(\d{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?|\d+(?:\.\d{1,2})?)");
                    if (m.Success)
                        decimal.TryParse(m.Groups[1].Value.Replace(",", "").Replace(" ", ""),
                            NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                }
            }

            if (string.IsNullOrWhiteSpace(name) || price <= 0) return null;

            return new ProductSearchResult
            {
                Name        = name,
                Description = description,
                Price       = price,
                Currency    = structured?.Currency
                    ?? doc.QuerySelector("meta[property='product:price:currency']")?.GetAttribute("content")
                    ?? doc.QuerySelector("meta[property='og:price:currency']")?.GetAttribute("content")
                    ?? ScraperHelpers.InferCurrencyCode(url),
                StoreName   = ScraperHelpers.InferStoreName(url),
                ProductUrl  = url,
                ImageUrl    = imageUrl,
                InStock     = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to scrape details for {Url}: {Message}", url, ex.Message);
            return null;
        }
    }
}