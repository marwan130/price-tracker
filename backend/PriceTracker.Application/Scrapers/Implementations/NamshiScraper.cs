namespace PriceTracker.Application.Scrapers.Implementations;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PriceTracker.Application.DTOs.Products;

public sealed class NamshiScraper : ISearchScraper
{
    private const int    MaxPerStore   = 24;
    private const string AlgoliaAppId  = "3HAEH3TQHX";
    private const string AlgoliaApiKey = "d5cd2d01f86a10e38d4fe36f6cd0735f";

    public IReadOnlyList<StoreDescriptor> Stores { get; } =
    [
        new("Namshi Egypt",        "EGP", "EG", ScraperKind.NamshiApi, "egypt"),
        new("Namshi Saudi Arabia", "SAR", "SA", ScraperKind.NamshiApi, "saudi"),
        new("Namshi UAE",          "AED", "AE", ScraperKind.NamshiApi, "uae"),
    ];

    private readonly IHttpClientFactory  _factory;
    private readonly ILogger<NamshiScraper> _logger;

    public NamshiScraper(IHttpClientFactory factory, ILogger<NamshiScraper> logger)
    {
        _factory = factory;
        _logger  = logger;
    }

    public async Task<List<ProductSearchResult>> ScrapeAsync(
        StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        var segment = store.SearchUrlTemplate;
        try
        {
            var client    = _factory.CreateClient("product-search");
            var indexName = $"namshi_{segment}_products";
            var url       = $"https://{AlgoliaAppId}-dsn.algolia.net/1/indexes/{indexName}/query";

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { query, hitsPerPage = MaxPerStore }),
                    Encoding.UTF8, "application/json")
            };
            req.Headers.Add("X-Algolia-Application-Id", AlgoliaAppId);
            req.Headers.Add("X-Algolia-API-Key",        AlgoliaApiKey);

            var json = await ScraperHelpers.SendAsync(client, req, ct);
            if (string.IsNullOrWhiteSpace(json)) return results;

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.ValueKind != JsonValueKind.Array)
                return results;

            var baseCountry = segment switch { "egypt" => "eg", "saudi" => "sa", _ => "ae" };

            foreach (var hit in hits.EnumerateArray())
            {
                if (results.Count >= MaxPerStore) break;

                var name = hit.TryGetProperty("name",         out var nEl) ? nEl.GetString()
                         : hit.TryGetProperty("product_name", out var n2)  ? n2.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;

                decimal price = 0;
                foreach (var f in new[] { "price", "sale_price", "special_price", "final_price" })
                    if (hit.TryGetProperty(f, out var pEl) && pEl.ValueKind == JsonValueKind.Number)
                    { price = pEl.GetDecimal(); break; }
                if (price <= 0) continue;

                var slug = hit.TryGetProperty("url_key", out var uEl) ? uEl.GetString()
                         : hit.TryGetProperty("sku",     out var sEl) ? sEl.GetString() : null;

                results.Add(new ProductSearchResult
                {
                    Name        = name,
                    Description = $"From {store.Name}.",
                    Price       = price,
                    Currency    = store.Currency,
                    StoreName   = store.Name,
                    ProductUrl  = string.IsNullOrWhiteSpace(slug)
                        ? $"https://en.namshi.com/{baseCountry}"
                        : $"https://en.namshi.com/{baseCountry}/{slug}.html",
                    ImageUrl    = hit.TryGetProperty("thumbnail_url", out var imgEl) ? imgEl.GetString() ?? string.Empty : string.Empty,
                    InStock     = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Namshi scrape failed for {Store}: {Msg}", store.Name, ex.Message);
        }
        return results;
    }
}