namespace PriceTracker.Application.Scrapers.Implementations;

using System.Globalization;
using System.Text.RegularExpressions;
using PriceTracker.Application.DTOs.Products;

public sealed class AmazonScraper : ISearchScraper
{
    private const int MaxPerStore = 24;

    public IReadOnlyList<StoreDescriptor> Stores { get; } =
    [
        new("Amazon Egypt",        "EGP", "EG", ScraperKind.AmazonHtml, "https://www.amazon.eg/s?k={q}&page={p}"),
        new("Amazon Saudi Arabia", "SAR", "SA", ScraperKind.AmazonHtml, "https://www.amazon.sa/s?k={q}&page={p}"),
        new("Amazon UAE",          "AED", "AE", ScraperKind.AmazonHtml, "https://www.amazon.ae/s?k={q}&page={p}"),
    ];

    private readonly IHttpClientFactory _factory;

    public AmazonScraper(IHttpClientFactory factory) => _factory = factory;

    public async Task<List<ProductSearchResult>> ScrapeAsync(
        StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();

        for (var page = 1; page <= 2 && results.Count < MaxPerStore; page++)
        {
            if (page > 1) await ScraperHelpers.PoliteDelayAsync(ct);

            var url  = store.SearchUrlTemplate
                .Replace("{q}", Uri.EscapeDataString(query))
                .Replace("{p}", page.ToString());
            var html = await ScraperHelpers.FetchHtmlAsync(_factory, url, ct);
            if (string.IsNullOrWhiteSpace(html)) break;

            var before = results.Count;
            var doc    = ScraperHelpers.HtmlParser.ParseDocument(html);

            foreach (var item in doc.QuerySelectorAll("div[data-component-type='s-search-result']"))
            {
                if (results.Count >= MaxPerStore) break;

                var name = item.QuerySelector("h2 a span")?.TextContent?.Trim()
                        ?? item.QuerySelector("[data-cy='title-recipe-title']")?.TextContent?.Trim();
                var href = item.QuerySelector("h2 a")?.GetAttribute("href")
                        ?? item.QuerySelector("a.a-link-normal[href*='/dp/']")?.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href)) continue;

                var productUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? href : $"https://{new Uri(url).Host}{href}";
                var q = productUrl.IndexOf('?');
                if (q > 0) productUrl = productUrl[..q];

                decimal price = 0;
                var offscreen = item.QuerySelector(".a-price .a-offscreen")?.TextContent?.Trim();
                if (!string.IsNullOrWhiteSpace(offscreen))
                {
                    var m = Regex.Match(offscreen, @"[\d,]+(?:\.\d{1,2})?");
                    if (m.Success) decimal.TryParse(m.Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                }
                if (price <= 0)
                {
                    var whole = item.QuerySelector(".a-price-whole")?.TextContent?.Trim().Replace(",", "").TrimEnd('.');
                    var frac  = item.QuerySelector(".a-price-fraction")?.TextContent?.Trim();
                    if (!string.IsNullOrWhiteSpace(whole))
                        decimal.TryParse(
                            string.IsNullOrWhiteSpace(frac) ? whole : $"{whole}.{frac}",
                            NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                }
                if (price <= 0) continue;

                decimal? rating = null;
                var ratingAttr  = item.QuerySelector("span[aria-label*='out of']")?.GetAttribute("aria-label");
                if (ratingAttr is not null)
                {
                    var m = Regex.Match(ratingAttr, @"[\d.]+");
                    if (m.Success && decimal.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var r)) rating = r;
                }

                results.Add(new ProductSearchResult
                {
                    Name       = name,
                    Description= $"From {store.Name}.",
                    Price      = price,
                    Currency   = store.Currency,
                    StoreName  = store.Name,
                    ProductUrl = productUrl,
                    ImageUrl   = item.QuerySelector("img.s-image")?.GetAttribute("src") ?? string.Empty,
                    Rating     = rating,
                    InStock    = true
                });
            }
            if (results.Count == before) break;
        }
        return results;
    }
}