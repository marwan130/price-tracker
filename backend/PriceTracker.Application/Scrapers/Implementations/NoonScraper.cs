namespace PriceTracker.Application.Scrapers.Implementations;

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PriceTracker.Application.DTOs.Products;

public sealed class NoonScraper : ISearchScraper
{
    private const int MaxPerStore = 24;

    public IReadOnlyList<StoreDescriptor> Stores { get; } =
    [
        new("Noon Egypt",        "EGP", "EG", ScraperKind.NoonApi, "egypt-en"),
        new("Noon Saudi Arabia", "SAR", "SA", ScraperKind.NoonApi, "saudi-en"),
        new("Noon UAE",          "AED", "AE", ScraperKind.NoonApi, "uae-en"),
    ];

    private readonly IHttpClientFactory _factory;

    public NoonScraper(IHttpClientFactory factory) => _factory = factory;

    public async Task<List<ProductSearchResult>> ScrapeAsync(
        StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        var pathSeg = store.SearchUrlTemplate;
        var country = pathSeg switch { "egypt-en" => "eg", "saudi-en" => "sa", _ => "ae" };
        var client  = _factory.CreateClient("product-search");

        for (var page = 1; page <= 2 && results.Count < MaxPerStore; page++)
        {
            if (page > 1) await ScraperHelpers.PoliteDelayAsync(ct);

            var before = results.Count;

            var apiUrl = $"https://www.noon.com/api/catalog/search?q={Uri.EscapeDataString(query)}&page={page}&limit=24&country={country}&lang=en";
            using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            req.Headers.Add("x-country", country);
            req.Headers.Add("x-lang",    "en");
            req.Headers.Add("Referer",   $"https://www.noon.com/{pathSeg}/search/?q={Uri.EscapeDataString(query)}");
            req.Headers.Add("Accept",    "application/json, */*");

            var json = await ScraperHelpers.SendAsync(client, req, ct);
            if (!string.IsNullOrWhiteSpace(json) && TryParseHits(json, out var hitsEl))
            {
                ParseHits(hitsEl, pathSeg, store, results);
            }
            else
            {
                var htmlUrl = $"https://www.noon.com/{pathSeg}/search/?q={Uri.EscapeDataString(query)}&page={page}";
                var html    = await ScraperHelpers.FetchHtmlAsync(_factory, htmlUrl, ct);
                if (!string.IsNullOrWhiteSpace(html))
                {
                    var m = Regex.Match(html, @"<script id=""__NEXT_DATA__"" type=""application/json"">(.*?)</script>", RegexOptions.Singleline);
                    if (m.Success)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(m.Groups[1].Value);
                            var hits      = ScraperHelpers.FindArray(doc.RootElement, "hits");
                            if (hits.ValueKind == JsonValueKind.Array)
                                ParseHits(hits, pathSeg, store, results);
                        }
                        catch { }
                    }
                }
            }

            if (results.Count == before) break;
        }
        return results;
    }

    private static bool TryParseHits(string json, out JsonElement hitsEl)
    {
        hitsEl = default;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("hits", out hitsEl) && hitsEl.ValueKind == JsonValueKind.Array)
                return true;
            if (doc.RootElement.TryGetProperty("data", out var d) && d.TryGetProperty("hits", out hitsEl) && hitsEl.ValueKind == JsonValueKind.Array)
                return true;
        }
        catch { }
        return false;
    }

    private static void ParseHits(
        JsonElement hits, string pathSeg, StoreDescriptor store, List<ProductSearchResult> results)
    {
        foreach (var hit in hits.EnumerateArray())
        {
            if (results.Count >= MaxPerStore) break;

            var name = hit.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(name)) continue;

            var sku = hit.TryGetProperty("product_sku", out var s1) ? s1.GetString()
                    : hit.TryGetProperty("sku",         out var s2) ? s2.GetString() : null;
            if (string.IsNullOrWhiteSpace(sku)) continue;

            decimal price = 0;
            foreach (var field in new[] { "price", "sale_price", "now_price", "priceSale", "was_price" })
            {
                if (!hit.TryGetProperty(field, out var pEl)) continue;
                if (pEl.ValueKind == JsonValueKind.Number) { price = pEl.GetDecimal(); break; }
                if (pEl.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(pEl.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pv))
                { price = pv; break; }
            }
            if (price <= 0) continue;

            var imgKey = hit.TryGetProperty("image_key", out var i1) ? i1.GetString()
                       : hit.TryGetProperty("imageKey",  out var i2) ? i2.GetString()
                       : hit.TryGetProperty("thumbnail", out var i3) ? i3.GetString() : null;

            decimal? rating = null;
            if (hit.TryGetProperty("avg_rating", out var rEl) || hit.TryGetProperty("rating", out rEl))
                if (rEl.ValueKind == JsonValueKind.Number) rating = rEl.GetDecimal();

            results.Add(new ProductSearchResult
            {
                Name       = name,
                Description= $"From {store.Name}.",
                Price      = price,
                Currency   = store.Currency,
                StoreName  = store.Name,
                ProductUrl = $"https://www.noon.com/{pathSeg}/{sku}/p/",
                ImageUrl   = string.IsNullOrWhiteSpace(imgKey) ? string.Empty : $"https://f.nooncdn.com/p/{imgKey}.jpg",
                Rating     = rating,
                InStock    = true
            });
        }
    }
}