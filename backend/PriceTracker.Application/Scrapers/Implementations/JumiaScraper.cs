namespace PriceTracker.Application.Scrapers.Implementations;

using System.Globalization;
using System.Text.RegularExpressions;
using PriceTracker.Application.DTOs.Products;

public sealed class JumiaScraper : ISearchScraper
{
    private const int MaxPerStore = 24;

    public IReadOnlyList<StoreDescriptor> Stores { get; } =
    [
        new("Jumia Egypt",        "EGP", "EG", ScraperKind.JumiaHtml, "https://www.jumia.com.eg/catalog/?q={q}&page={p}"),
        new("Jumia Saudi Arabia", "SAR", "SA", ScraperKind.JumiaHtml, "https://www.jumia.com.sa/catalog/?q={q}&page={p}"),
    ];

    private readonly IHttpClientFactory _factory;

    public JumiaScraper(IHttpClientFactory factory) => _factory = factory;

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
            var baseUri = new Uri(url);

            foreach (var item in doc.QuerySelectorAll("article.prd"))
            {
                if (results.Count >= MaxPerStore) break;

                var name = item.QuerySelector(".name")?.TextContent?.Trim();
                var href = item.QuerySelector("a.core")?.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href)) continue;

                var priceTxt = item.QuerySelector(".prc")?.TextContent;
                var pm       = Regex.Match(priceTxt ?? "", @"[\d,]+");
                if (!pm.Success || !decimal.TryParse(pm.Value.Replace(",", ""), out var price) || price <= 0) continue;

                var productUrl = href.StartsWith("http") ? href : $"{baseUri.Scheme}://{baseUri.Host}{href}";

                decimal? rating = null;
                var styleAttr = item.QuerySelector(".stars._s")?.GetAttribute("style");
                if (!string.IsNullOrWhiteSpace(styleAttr))
                {
                    var rm = Regex.Match(styleAttr, @"([\d.]+)%");
                    if (rm.Success && decimal.TryParse(rm.Value.TrimEnd('%'), NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                        rating = Math.Round(pct / 20m, 1);
                }

                int? reviewCount = null;
                var revText = item.QuerySelector(".rev")?.TextContent?.Trim();
                if (!string.IsNullOrWhiteSpace(revText))
                {
                    var rvm = Regex.Match(revText, @"\d+");
                    if (rvm.Success && int.TryParse(rvm.Value, out var rv)) reviewCount = rv;
                }

                results.Add(new ProductSearchResult
                {
                    Name        = name,
                    Description = $"From {store.Name}.",
                    Price       = price,
                    Currency    = store.Currency,
                    StoreName   = store.Name,
                    ProductUrl  = productUrl,
                    ImageUrl    = item.QuerySelector("img.img")?.GetAttribute("data-src")
                               ?? item.QuerySelector("img.img")?.GetAttribute("src") ?? string.Empty,
                    Rating      = rating,
                    ReviewCount = reviewCount,
                    InStock     = true
                });
            }
            if (results.Count == before) break;
        }
        return results;
    }
}