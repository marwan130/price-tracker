namespace PriceTracker.Application.Scrapers.Implementations;

using System.Globalization;
using System.Text.RegularExpressions;
using PriceTracker.Application.DTOs.Products;
using Microsoft.Extensions.Logging;

public sealed class GenericHtmlScraper : ISearchScraper
{
    private const int MaxPerStore = 24;

    private static readonly string[] ProductSelectors =
    [
        "div.product-item", "div.product", "div.item", "article.product", "li.product",
        "[data-product]", ".product-card", ".product-tile", ".product-box"
    ];

    public IReadOnlyList<StoreDescriptor> Stores { get; } =
    [
        new("B.TECH",               "EGP", "EG", ScraperKind.GenericHtml, "https://btech.com/en/search?q={q}"),
        new("2B",                   "EGP", "EG", ScraperKind.GenericHtml, "https://www.2b.com.eg/en/search?q={q}"),
        new("Raya Shop",            "EGP", "EG", ScraperKind.GenericHtml, "https://www.rayashop.com/en/catalogsearch/result/?q={q}"),
        new("Dream 2000",           "EGP", "EG", ScraperKind.GenericHtml, "https://www.dream2000.com.eg/search?q={q}"),
        new("ElAraby",              "EGP", "EG", ScraperKind.GenericHtml, "https://www.elarabygroup.com/en/search?q={q}"),
        new("Sigma Computer",       "EGP", "EG", ScraperKind.GenericHtml, "https://www.sigma-computer.com/search?q={q}"),
        new("CompuMe",              "EGP", "EG", ScraperKind.GenericHtml, "https://www.compume.com/search?q={q}"),
        new("Carrefour Egypt",      "EGP", "EG", ScraperKind.GenericHtml, "https://www.carrefouregypt.com/mafegy/en/search?q={q}"),
        new("Ubuy Egypt",           "EGP", "EG", ScraperKind.GenericHtml, "https://www.ubuy.com.eg/en/search/?q={q}"),
        new("Jarir Bookstore",      "SAR", "SA", ScraperKind.GenericHtml, "https://www.jarir.com/sa-en/catalogsearch/result/?q={q}"),
        new("eXtra",                "SAR", "SA", ScraperKind.GenericHtml, "https://www.extra.com/en-sa/search#{q}"),
        new("SACO",                 "SAR", "SA", ScraperKind.GenericHtml, "https://www.saco.sa/en/search?q={q}"),
        new("X-cite KSA",           "SAR", "SA", ScraperKind.GenericHtml, "https://www.xcite.com/search?q={q}"),
        new("Carrefour KSA",        "SAR", "SA", ScraperKind.GenericHtml, "https://www.carrefourksa.com/ksa/en/search?q={q}"),
        new("Lulu Hypermarket KSA", "SAR", "SA", ScraperKind.GenericHtml, "https://www.luluhypermarket.com/en-sa/search?q={q}"),
        new("Ubuy Saudi Arabia",    "SAR", "SA", ScraperKind.GenericHtml, "https://www.ubuy.com.sa/en/search/?q={q}"),
        new("Sharaf DG",            "AED", "AE", ScraperKind.GenericHtml, "https://www.sharafdg.com/search/?q={q}"),
        new("Jumbo Electronics",    "AED", "AE", ScraperKind.GenericHtml, "https://www.jumbo.ae/search?q={q}"),
        new("Emax",                 "AED", "AE", ScraperKind.GenericHtml, "https://www.emaxonline.com/search?q={q}"),
        new("Virgin Megastore UAE", "AED", "AE", ScraperKind.GenericHtml, "https://www.virginmegastore.ae/search?q={q}"),
        new("Plug Ins UAE",         "AED", "AE", ScraperKind.GenericHtml, "https://www.pluginsuae.com/search?q={q}"),
        new("Geekay UAE",           "AED", "AE", ScraperKind.GenericHtml, "https://www.geekay.com/catalogsearch/result/?q={q}"),
        new("Carrefour UAE",        "AED", "AE", ScraperKind.GenericHtml, "https://www.carrefouruae.com/uae/en/search?q={q}"),
        new("Lulu Hypermarket UAE", "AED", "AE", ScraperKind.GenericHtml, "https://www.luluhypermarket.com/en-ae/search?q={q}"),
        new("Homzmart UAE",         "AED", "AE", ScraperKind.GenericHtml, "https://www.homzmart.com/en/search?q={q}"),
        new("Ubuy UAE",             "AED", "AE", ScraperKind.GenericHtml, "https://www.ubuy.ae/en/search/?q={q}"),
    ];

    private readonly IHttpClientFactory         _factory;
    private readonly ILogger<GenericHtmlScraper> _logger;

    public GenericHtmlScraper(IHttpClientFactory factory, ILogger<GenericHtmlScraper> logger)
    {
        _factory = factory;
        _logger  = logger;
    }

    public async Task<List<ProductSearchResult>> ScrapeAsync(
        StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        try
        {
            var url  = store.SearchUrlTemplate.Replace("{q}", Uri.EscapeDataString(query));
            var html = await ScraperHelpers.FetchHtmlAsync(_factory, url, ct);
            if (string.IsNullOrWhiteSpace(html)) return results;

            var doc     = ScraperHelpers.HtmlParser.ParseDocument(html);
            var baseUri = new Uri(url);

            foreach (var selector in ProductSelectors)
            {
                foreach (var item in doc.QuerySelectorAll(selector))
                {
                    if (results.Count >= MaxPerStore) break;

                    var name = item.QuerySelector(".name, .title, .product-name, h2, h3, h4")?.TextContent?.Trim();
                    var href = item.QuerySelector("a")?.GetAttribute("href");
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href)) continue;

                    var priceTxt = item.QuerySelector(".price, .prc, .amount, [data-price]")?.TextContent;
                    var pm = Regex.Match(priceTxt ?? "", @"[\d,]+\.?\d*");
                    if (!pm.Success || !decimal.TryParse(pm.Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0) continue;

                    results.Add(new ProductSearchResult
                    {
                        Name        = name,
                        Description = $"From {store.Name}.",
                        Price       = price,
                        Currency    = store.Currency,
                        StoreName   = store.Name,
                        ProductUrl  = href.StartsWith("http") ? href : $"{baseUri.Scheme}://{baseUri.Host}{href}",
                        ImageUrl    = item.QuerySelector("img")?.GetAttribute("src") ?? string.Empty,
                        InStock     = true
                    });
                }
                if (results.Count > 0) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Generic HTML scrape failed for {Store}: {Msg}", store.Name, ex.Message);
        }
        return results;
    }
}