namespace PriceTracker.Application.Services;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Hangfire;
using Microsoft.Extensions.Logging;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;

public enum ScraperKind
{
    AmazonHtml,    
    NoonApi,       
    JumiaHtml,     
    TemuApi,       
    AliExpressApi, 
    EbayHtml,     
    NamshiApi,     
    SixthStreetApi,
    FarfetchApi,  
    GenericHtml,   
    SkipBrowser,   
}

public sealed record StoreDescriptor(
    string Name,
    string Currency,
    string Country,
    ScraperKind Kind,
    string SearchUrlTemplate,
    string? ApiKey = null,
    string? AppId = null);

public class ProductSearchService : IProductSearchService
{
    private const int MaxPerStore  = 24;
    private const int TimeoutSecs  = 10;

    private static readonly ConcurrentDictionary<string, CachedSearchResult> RecentSearchResults = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan SearchResultCacheLifetime = TimeSpan.FromMinutes(30);

    private static readonly ConcurrentDictionary<string, DateTime> _recentlyScraped = new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<ProductSearchService> _logger;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IListingRepository _listingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly HtmlParser _parser = new();

    public static readonly StoreDescriptor[] AllStores =
    [
        new("Amazon Egypt",         "EGP", "EG", ScraperKind.AmazonHtml,    "https://www.amazon.eg/s?k={q}&page={p}"),
        new("Amazon Saudi Arabia",  "SAR", "SA", ScraperKind.AmazonHtml,    "https://www.amazon.sa/s?k={q}&page={p}"),
        new("Amazon UAE",           "AED", "AE", ScraperKind.AmazonHtml,    "https://www.amazon.ae/s?k={q}&page={p}"),
        new("Noon Egypt",           "EGP", "EG", ScraperKind.NoonApi,       "egypt-en"),
        new("Noon Saudi Arabia",    "SAR", "SA", ScraperKind.NoonApi,       "saudi-en"),
        new("Noon UAE",             "AED", "AE", ScraperKind.NoonApi,       "uae-en"),
        new("Jumia Egypt",          "EGP", "EG", ScraperKind.JumiaHtml,     "https://www.jumia.com.eg/catalog/?q={q}&page={p}"),
        new("Jumia Saudi Arabia",   "SAR", "SA", ScraperKind.JumiaHtml,     "https://www.jumia.com.sa/catalog/?q={q}&page={p}"),
        new("Temu",                 "USD", "GL", ScraperKind.TemuApi,       "https://www.temu.com/api/phantom/search/result"),
        new("AliExpress",           "USD", "GL", ScraperKind.AliExpressApi, "https://www.aliexpress.com/wholesale?SearchText={q}&page={p}"),
        new("eBay",                 "USD", "GL", ScraperKind.EbayHtml,      "https://www.ebay.com/sch/i.html?_nkw={q}&_pgn={p}"),
        new("Jarir Bookstore",      "SAR", "SA", ScraperKind.GenericHtml,   "https://www.jarir.com/sa-en/catalogsearch/result/?q={q}"),
        new("eXtra",                "SAR", "SA", ScraperKind.GenericHtml,   "https://www.extra.com/en-sa/search#{q}"),
        new("SACO",                 "SAR", "SA", ScraperKind.GenericHtml,   "https://www.saco.sa/en/search?q={q}"),
        new("X-cite KSA",           "SAR", "SA", ScraperKind.GenericHtml,   "https://www.xcite.com/search?q={q}"),
        new("Lulu Electronics KSA", "SAR", "SA", ScraperKind.GenericHtml,   "https://www.luluhypermarket.com/en-sa/search?q={q}"),
        new("Sharaf DG",            "AED", "AE", ScraperKind.GenericHtml,   "https://www.sharafdg.com/search/?q={q}"),
        new("Jumbo Electronics",    "AED", "AE", ScraperKind.GenericHtml,   "https://www.jumbo.ae/search?q={q}"),
        new("Emax",                 "AED", "AE", ScraperKind.GenericHtml,   "https://www.emaxonline.com/search?q={q}"),
        new("Virgin Megastore UAE", "AED", "AE", ScraperKind.GenericHtml,   "https://www.virginmegastore.ae/search?q={q}"),
        new("EROS Digital Home",    "AED", "AE", ScraperKind.GenericHtml,   "https://www.erosdigitalhome.com/search?q={q}"),
        new("Plug Ins",             "AED", "AE", ScraperKind.GenericHtml,   "https://www.pluginsuae.com/search?q={q}"),
        new("B.TECH",               "EGP", "EG", ScraperKind.GenericHtml,   "https://btech.com/en/search?q={q}"),
        new("2B",                   "EGP", "EG", ScraperKind.GenericHtml,   "https://www.2b.com.eg/en/search?q={q}"),
        new("Raya Shop",            "EGP", "EG", ScraperKind.GenericHtml,   "https://www.rayashop.com/en/catalogsearch/result/?q={q}"),
        new("Dream 2000",           "EGP", "EG", ScraperKind.GenericHtml,   "https://www.dream2000.com.eg/search?q={q}"),
        new("ElAraby",              "EGP", "EG", ScraperKind.GenericHtml,   "https://www.elarabygroup.com/en/search?q={q}"),
        new("Sigma Computer",       "EGP", "EG", ScraperKind.GenericHtml,   "https://www.sigma-computer.com/search?q={q}"),
        new("CompuMe",              "EGP", "EG", ScraperKind.GenericHtml,   "https://www.compume.com/search?q={q}"),
        new("ElNekhely Technology", "EGP", "EG", ScraperKind.GenericHtml,   "https://www.elnkhely.com/search?q={q}"),
        new("Technology Valley",    "EGP", "EG", ScraperKind.GenericHtml,   "https://www.technologyvalley.com/search?q={q}"),
        new("Namshi KSA",           "SAR", "SA", ScraperKind.NamshiApi,     "sa"),
        new("Namshi UAE",           "AED", "AE", ScraperKind.NamshiApi,     "ae"),
        new("6thStreet KSA",        "SAR", "SA", ScraperKind.SixthStreetApi, "SA"),
        new("6thStreet UAE",        "AED", "AE", ScraperKind.SixthStreetApi, "AE"),
        new("6thStreet Egypt",      "EGP", "EG", ScraperKind.SixthStreetApi, "EG"),
        new("Centrepoint",          "AED", "GL", ScraperKind.GenericHtml,   "https://www.centrepointstores.com/ae/en/search?q={q}"),
        new("Max Fashion",          "AED", "GL", ScraperKind.GenericHtml,   "https://www.maxfashion.com/ae/en/search?q={q}"),
        new("Splash",               "AED", "GL", ScraperKind.GenericHtml,   "https://www.splashfashions.com/ae/en/search?q={q}"),
        new("Ounass",               "AED", "AE", ScraperKind.GenericHtml,   "https://www.ounass.ae/search?q={q}"),
        new("Golden Scent",         "SAR", "SA", ScraperKind.GenericHtml,   "https://www.goldenscent.com/en/search?q={q}"),
        new("Nice One",             "SAR", "SA", ScraperKind.GenericHtml,   "https://www.niceonesa.com/en/search?q={q}"),
        new("Styli",                "AED", "GL", ScraperKind.GenericHtml,   "https://www.styli.com/ae-en/search?q={q}"),
        new("Brands For Less",      "AED", "AE", ScraperKind.GenericHtml,   "https://www.brandsforless.com/en-ae/search?q={q}"),
        new("Sivvi",                "AED", "AE", ScraperKind.GenericHtml,   "https://www.sivvi.com/en/search?q={q}"),
        new("Farfetch",             "USD", "GL", ScraperKind.FarfetchApi,   "https://www.farfetch.com"),
        new("LC Waikiki Egypt",     "EGP", "EG", ScraperKind.GenericHtml,   "https://www.lcwaikiki.com/en-EG/search?q={q}"),
        new("Defacto Egypt",        "EGP", "EG", ScraperKind.GenericHtml,   "https://www.defacto.com/en/search?q={q}"),
        new("Beymen Egypt",         "EGP", "EG", ScraperKind.GenericHtml,   "https://www.beymen.com/en/search?q={q}"),
        new("Town Team",            "EGP", "EG", ScraperKind.GenericHtml,   "https://townteam.com/search?q={q}"),
        new("Dice",                 "EGP", "EG", ScraperKind.GenericHtml,   "https://diceegypt.com/search?q={q}"),
        new("Sun & Sand Sports",    "AED", "GL", ScraperKind.GenericHtml,   "https://www.sssports.com/search?q={q}"),
        new("Decathlon",            "AED", "GL", ScraperKind.GenericHtml,   "https://www.decathlon.ae/search?q={q}"),
        new("Go Sport",             "AED", "GL", ScraperKind.GenericHtml,   "https://www.gosport.ae/search?q={q}")
    ];

    public ProductSearchService(
        ILogger<ProductSearchService> logger,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IStoreRepository storeRepository,
        IListingRepository listingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        IHttpClientFactory httpClientFactory,
        IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _storeRepository = storeRepository;
        _listingRepository = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _httpClientFactory = httpClientFactory;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(ProductFilterRequest filter, CancellationToken ct = default)
    {
        var query = filter.Query?.Trim();
        _logger.LogInformation("Searching for products: {Query}", query);

        if (string.IsNullOrWhiteSpace(query))
            return [];

        var liveResults = await ScrapeAllAsync(query, ct);
        _logger.LogInformation("Live scrape returned {Count} raw results for '{Query}'", liveResults.Count, query);

        _backgroundJobClient.Enqueue(() => BackgroundScrapeAsync(query));

        await MergeExistingListingsAsync(liveResults);

        var filteredResults = (await ApplyFiltersAsync(liveResults, filter))
            .Where(r => r.Price > 0 && !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(r => NormalizeProductUrl(r.ProductUrl), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (filteredResults.Count == 0)
        {
            var localResults = new List<ProductSearchResult>();
            await AddLocalResultsAsync(localResults, filter);
            filteredResults = (await ApplyFiltersAsync(localResults, filter))
                .Where(r => r.Price > 0 && !string.IsNullOrWhiteSpace(r.Name))
                .GroupBy(r => NormalizeProductUrl(r.ProductUrl), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        CacheSearchResults(filteredResults);
        return SortResults(filteredResults, filter.SortBy);
    }

    public async Task<ProductSearchResult?> SearchByUrlAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching for product by URL: {Url}", url);

        if (string.IsNullOrWhiteSpace(url))
            return null;

        var normalizedUrl = NormalizeProductUrl(url);
        if (RecentSearchResults.TryGetValue(normalizedUrl, out var cached))
        {
            if (cached.ExpiresAt > DateTime.UtcNow)
                return CloneSearchResult(cached.Result, url);
            RecentSearchResults.TryRemove(normalizedUrl, out _);
        }

        return await ScrapeProductDetailsAsync(url);
    }

    public async Task BackgroundScrapeAsync(string query)
    {
        _logger.LogInformation("Background scrape persisting results for '{Query}'", query);

        var liveResults = await ScrapeAllAsync(query, CancellationToken.None);

        var resultsToInsert = liveResults
            .Where(r => r.Price > 0 && !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(r => NormalizeProductUrl(r.ProductUrl), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var existingListings = (await _listingRepository.GetAllAsync())
            .Where(l => !string.IsNullOrWhiteSpace(l.ProductUrl))
            .ToDictionary(l => NormalizeProductUrl(l.ProductUrl), StringComparer.OrdinalIgnoreCase);

        var stores    = await _storeRepository.GetAllAsync();
        var storeDict = stores.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        var storeDescs = AllStores.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var result in resultsToInsert)
        {
            var normUrl = NormalizeProductUrl(result.ProductUrl);
            if (existingListings.TryGetValue(normUrl, out var existingListing))
            {
                await RecordLatestPriceAsync(existingListing, result);
                continue;
            }

            if (!storeDict.TryGetValue(result.StoreName, out var storeObj))
            {
                var desc = storeDescs.GetValueOrDefault(result.StoreName);
                storeObj = new Store
                {
                    StoreId  = Guid.NewGuid(),
                    Name     = result.StoreName,
                    BaseUrl  = BuildBaseUrl(result.StoreName),
                    Country  = desc?.Country ?? "AE"
                };
                await _storeRepository.AddAsync(storeObj);
                storeDict[storeObj.Name] = storeObj;
            }

            var product = new Product
            {
                ProductId   = Guid.NewGuid(),
                Name        = result.Name,
                Description = result.Description,
                Brand       = result.StoreName,
                CategoryId  = null
            };

            var listing = new StoreProductListing
            {
                ListingId      = Guid.NewGuid(),
                ProductId      = product.ProductId,
                StoreId        = storeObj.StoreId,
                ProductUrl     = result.ProductUrl,
                IsActive       = true,
                LastScrapedAt  = DateTime.UtcNow
            };

            listing.PriceHistories.Add(new PriceHistory
            {
                ListingId    = listing.ListingId,
                Price        = result.Price,
                CurrencyCode = result.Currency ?? "USD",
                RecordedAt   = DateTime.UtcNow,
                ScrapedAt    = DateTime.UtcNow
            });

            product.Listings.Add(listing);

            if (!string.IsNullOrWhiteSpace(result.ImageUrl))
            {
                product.Images.Add(new ProductImage
                {
                    ProductId = product.ProductId,
                    Url       = result.ImageUrl,
                    IsPrimary = true
                });
            }

            await _productRepository.AddAsync(product);
        }

        _logger.LogInformation("Background scrape persisted {Count} new products for '{Query}'", resultsToInsert.Count, query);
    }

    private async Task<List<ProductSearchResult>> ScrapeAllAsync(string query, CancellationToken ct = default)
    {
        var tasks = AllStores.Select(store => ScrapeStoreAsync(store, query, ct));
        var settled = await Task.WhenAll(tasks);
        return settled.SelectMany(r => r).ToList();
    }

    private Task<List<ProductSearchResult>> ScrapeStoreAsync(string storeName, string query, CancellationToken ct = default)
    {
        var desc = Array.Find(AllStores, s => string.Equals(s.Name, storeName, StringComparison.OrdinalIgnoreCase));
        return desc is null ? Task.FromResult(new List<ProductSearchResult>()) : ScrapeStoreAsync(desc, query, ct);
    }

    private async Task<List<ProductSearchResult>> ScrapeStoreAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        try
        {
            return store.Kind switch
            {
                ScraperKind.AmazonHtml    => await ScrapeAmazonAsync(store, query, ct),
                ScraperKind.NoonApi       => await ScrapeNoonAsync(store, query, ct),
                ScraperKind.JumiaHtml     => await ScrapeJumiaAsync(store, query, ct),
                ScraperKind.TemuApi       => await ScrapeTemuAsync(store, query, ct),
                ScraperKind.AliExpressApi => await ScrapeAliExpressAsync(store, query, ct),
                ScraperKind.EbayHtml      => await ScrapeEbayAsync(store, query, ct),
                ScraperKind.NamshiApi     => await ScrapeNamshiAsync(store, query, ct),
                ScraperKind.SixthStreetApi=> await ScrapeSixthStreetAsync(store, query, ct),
                ScraperKind.FarfetchApi   => await ScrapeFarfetchAsync(store, query, ct),
                ScraperKind.GenericHtml   => await ScrapeGenericHtmlAsync(store, query, ct),
                ScraperKind.SkipBrowser   => [],
                _                         => []
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scraper failed for {Store}", store.Name);
            return [];
        }
    }

    private async Task<List<ProductSearchResult>> ScrapeAmazonAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        var client  = _httpClientFactory.CreateClient("product-search");

        for (var page = 1; page <= 2 && results.Count < MaxPerStore; page++)
        {
            var url = store.SearchUrlTemplate.Replace("{q}", Uri.EscapeDataString(query)).Replace("{p}", page.ToString());
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("Referer", url);
            req.Headers.Add("DNT", "1");

            var html = await SendAsync(client, req, ct);
            if (string.IsNullOrWhiteSpace(html)) break;

            var before = results.Count;
            var doc    = _parser.ParseDocument(html);

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
                        decimal.TryParse(string.IsNullOrWhiteSpace(frac) ? whole : $"{whole}.{frac}",
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
                    Name        = name,
                    Description = $"From {store.Name}.",
                    Price       = price,
                    Currency    = store.Currency,
                    StoreName   = store.Name,
                    ProductUrl  = productUrl,
                    ImageUrl    = item.QuerySelector("img.s-image")?.GetAttribute("src") ?? string.Empty,
                    Rating      = rating,
                    InStock     = true
                });
            }
            if (results.Count == before) break;
        }
        return results;
    }

    private async Task<List<ProductSearchResult>> ScrapeNoonAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results    = new List<ProductSearchResult>();
        var pathSeg    = store.SearchUrlTemplate;
        var country    = pathSeg switch { "egypt-en" => "eg", "saudi-en" => "sa", _ => "ae" };
        var client     = _httpClientFactory.CreateClient("product-search");

        for (var page = 1; page <= 2 && results.Count < MaxPerStore; page++)
        {
            var before   = results.Count;
            var apiUrl   = $"https://www.noon.com/api/catalog/search?q={Uri.EscapeDataString(query)}&page={page}&limit=24&country={country}&lang=en";
            using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            req.Headers.Add("x-country", country);
            req.Headers.Add("x-lang", "en");
            req.Headers.Add("Referer", $"https://www.noon.com/{pathSeg}/search/?q={Uri.EscapeDataString(query)}");
            req.Headers.Add("Accept", "application/json, */*");

            bool parsed = false;
            var json    = await SendAsync(client, req, ct);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var hitsEl    = default(JsonElement);
                    if (doc.RootElement.TryGetProperty("hits", out hitsEl)
                     || (doc.RootElement.TryGetProperty("data", out var d) && d.TryGetProperty("hits", out hitsEl)))
                    {
                        if (hitsEl.ValueKind == JsonValueKind.Array)
                        { ParseNoonHits(hitsEl, pathSeg, store, results); parsed = true; }
                    }
                }
                catch {}
            }

            if (!parsed)
            {
                var htmlUrl  = $"https://www.noon.com/{pathSeg}/search/?q={Uri.EscapeDataString(query)}&page={page}";
                var html     = await FetchHtmlAsync(htmlUrl, ct);
                if (!string.IsNullOrWhiteSpace(html))
                {
                    var m = Regex.Match(html, @"<script id=""__NEXT_DATA__"" type=""application/json"">(.*?)</script>", RegexOptions.Singleline);
                    if (m.Success)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(m.Groups[1].Value);
                            var hits      = FindArray(doc.RootElement, "hits");
                            if (hits.ValueKind == JsonValueKind.Array)
                                ParseNoonHits(hits, pathSeg, store, results);
                        }
                        catch {}
                    }
                }
            }
            if (results.Count == before) break;
        }
        return results;
    }

    private void ParseNoonHits(JsonElement hits, string pathSeg, StoreDescriptor store, List<ProductSearchResult> results)
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
                if (pEl.ValueKind == JsonValueKind.String && decimal.TryParse(pEl.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pv)) { price = pv; break; }
            }
            if (price <= 0) continue;

            var imgKey  = hit.TryGetProperty("image_key", out var i1) ? i1.GetString()
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

    private async Task<List<ProductSearchResult>> ScrapeJumiaAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        for (var page = 1; page <= 2 && results.Count < MaxPerStore; page++)
        {
            var url    = store.SearchUrlTemplate.Replace("{q}", Uri.EscapeDataString(query)).Replace("{p}", page.ToString());
            var html   = await FetchHtmlAsync(url, ct);
            if (string.IsNullOrWhiteSpace(html)) break;

            var before = results.Count;
            var doc    = _parser.ParseDocument(html);

            foreach (var item in doc.QuerySelectorAll("article.prd"))
            {
                if (results.Count >= MaxPerStore) break;
                var name = item.QuerySelector(".name")?.TextContent?.Trim();
                var href = item.QuerySelector("a.core")?.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href)) continue;

                var priceTxt = item.QuerySelector(".prc")?.TextContent;
                var pm       = Regex.Match(priceTxt ?? "", @"[\d,]+");
                if (!pm.Success || !decimal.TryParse(pm.Value.Replace(",", ""), out var price) || price <= 0) continue;

                var baseUrl  = new Uri(url);
                var productUrl = href.StartsWith("http") ? href : $"{baseUrl.Scheme}://{baseUrl.Host}{href}";

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

    private async Task<List<ProductSearchResult>> ScrapeTemuAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        try
        {
            var client   = _httpClientFactory.CreateClient("product-search");
            var payload  = JsonSerializer.Serialize(new
            {
                keyword  = query,
                listSort = 0,
                pageNo   = 1,
                pageSize = 20,
                timezone = "Africa/Cairo"
            });
            using var req = new HttpRequestMessage(HttpMethod.Post, store.SearchUrlTemplate)
            {
                Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
            };
            req.Headers.Add("Referer", "https://www.temu.com/");
            req.Headers.Add("Accept", "application/json");

            using var cts    = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSecs));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            var resp         = await client.SendAsync(req, linked.Token);
            if (!resp.IsSuccessStatusCode) return results;

            var json = await resp.Content.ReadAsStringAsync(linked.Token);
            using var doc = JsonDocument.Parse(json);

            var goodsList = FindArray(doc.RootElement, "goodsList");
            if (goodsList.ValueKind != JsonValueKind.Array)
                goodsList = FindArray(doc.RootElement, "goods_list");

            foreach (var item in goodsList.EnumerateArray())
            {
                if (results.Count >= MaxPerStore) break;
                var name  = item.TryGetProperty("goods_name",  out var n1) ? n1.GetString()
                          : item.TryGetProperty("goodsName",   out var n2) ? n2.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;

                decimal price = 0;
                foreach (var f in new[] { "price", "salePrice", "sale_price", "min_price" })
                    if (item.TryGetProperty(f, out var pEl) && pEl.ValueKind == JsonValueKind.Number) { price = pEl.GetDecimal() / 100m; break; }
                if (price <= 0) continue;

                var goodsId = item.TryGetProperty("goods_id", out var gEl) ? gEl.ToString() : null;
                var imgUrl  = item.TryGetProperty("goods_thumb_url", out var iEl) ? iEl.GetString()
                            : item.TryGetProperty("image_url", out var iEl2) ? iEl2.GetString() : string.Empty;

                results.Add(new ProductSearchResult
                {
                    Name       = name,
                    Description= $"From Temu.",
                    Price      = price,
                    Currency   = "USD",
                    StoreName  = "Temu",
                    ProductUrl = string.IsNullOrWhiteSpace(goodsId) ? "https://www.temu.com" : $"https://www.temu.com/goods.html?goods_id={goodsId}",
                    ImageUrl   = imgUrl ?? string.Empty,
                    InStock    = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Temu scrape failed: {Msg}", ex.Message);
        }
        return results;
    }

    private async Task<List<ProductSearchResult>> ScrapeAliExpressAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        try
        {
            for (var page = 1; page <= 2 && results.Count < MaxPerStore; page++)
            {
                var url  = store.SearchUrlTemplate.Replace("{q}", Uri.EscapeDataString(query)).Replace("{p}", page.ToString());
                var html = await FetchHtmlAsync(url, ct);
                if (string.IsNullOrWhiteSpace(html)) break;

                var before = results.Count;
                var doc    = _parser.ParseDocument(html);

                foreach (var item in doc.QuerySelectorAll("[data-product-id]"))
                {
                    if (results.Count >= MaxPerStore) break;

                    var name = item.QuerySelector("h3")?.TextContent?.Trim()
                             ?? item.QuerySelector("[class*='title']")?.TextContent?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var priceEl = item.QuerySelector("[class*='price--current']")
                               ?? item.QuerySelector("[class*='Price--current']")
                               ?? item.QuerySelector("[class*='price']");
                    var priceTxt = priceEl?.TextContent?.Trim();
                    var pm = Regex.Match(priceTxt ?? "", @"[\d.]+");
                    if (!pm.Success || !decimal.TryParse(pm.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0) continue;

                    var productId = item.GetAttribute("data-product-id") ?? "";
                    var href      = item.QuerySelector("a")?.GetAttribute("href") ?? "";
                    var productUrl = href.StartsWith("http") ? href
                        : string.IsNullOrWhiteSpace(productId) ? "https://www.aliexpress.com"
                        : $"https://www.aliexpress.com/item/{productId}.html";

                    results.Add(new ProductSearchResult
                    {
                        Name       = name,
                        Description= "From AliExpress.",
                        Price      = price,
                        Currency   = "USD",
                        StoreName  = "AliExpress",
                        ProductUrl = productUrl,
                        ImageUrl   = item.QuerySelector("img")?.GetAttribute("src") ?? string.Empty,
                        InStock    = true
                    });
                }
                if (results.Count == before) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AliExpress scrape failed: {Msg}", ex.Message);
        }
        return results;
    }

    private async Task<List<ProductSearchResult>> ScrapeEbayAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        for (var page = 1; page <= 2 && results.Count < MaxPerStore; page++)
        {
            var url  = store.SearchUrlTemplate.Replace("{q}", Uri.EscapeDataString(query)).Replace("{p}", page.ToString());
            var html = await FetchHtmlAsync(url, ct);
            if (string.IsNullOrWhiteSpace(html)) break;

            var before = results.Count;
            var doc    = _parser.ParseDocument(html);

            foreach (var item in doc.QuerySelectorAll("li.s-item"))
            {
                if (results.Count >= MaxPerStore) break;

                var name = item.QuerySelector(".s-item__title")?.TextContent?.Trim();
                var href = item.QuerySelector("a.s-item__link")?.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href) || name.Contains("Shop on eBay")) continue;

                var priceTxt = item.QuerySelector(".s-item__price")?.TextContent?.Trim();
                var pm = Regex.Match(priceTxt ?? "", @"[\d,]+\.?\d*");
                if (!pm.Success || !decimal.TryParse(pm.Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0) continue;

                var qIdx = href.IndexOf('?'); 
                var productUrl = qIdx > 0 ? href[..qIdx] : href;

                results.Add(new ProductSearchResult
                {
                    Name       = name,
                    Description= "From eBay.",
                    Price      = price,
                    Currency   = "USD",
                    StoreName  = "eBay",
                    ProductUrl = productUrl,
                    ImageUrl   = item.QuerySelector("img.s-item__image-img")?.GetAttribute("src") ?? string.Empty,
                    InStock    = true
                });
            }
            if (results.Count == before) break;
        }
        return results;
    }

    private async Task<List<ProductSearchResult>> ScrapeNamshiAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results  = new List<ProductSearchResult>();
        var countrySegment = store.SearchUrlTemplate; 
        try
        {
            var client = _httpClientFactory.CreateClient("product-search");
            var algoliaAppId  = "3HAEH3TQHX";
            var algoliaApiKey = "d5cd2d01f86a10e38d4fe36f6cd0735f";
            var indexName     = $"namshi_{countrySegment}_products";
            var algoliaUrl    = $"https://{algoliaAppId}-dsn.algolia.net/1/indexes/{indexName}/query";

            var body = JsonSerializer.Serialize(new { query, hitsPerPage = MaxPerStore });
            using var req = new HttpRequestMessage(HttpMethod.Post, algoliaUrl)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
            req.Headers.Add("X-Algolia-Application-Id", algoliaAppId);
            req.Headers.Add("X-Algolia-API-Key", algoliaApiKey);

            using var cts    = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSecs));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            var resp = await client.SendAsync(req, linked.Token);
            if (!resp.IsSuccessStatusCode) return results;

            var json = await resp.Content.ReadAsStringAsync(linked.Token);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.ValueKind != JsonValueKind.Array)
                return results;

            foreach (var hit in hits.EnumerateArray())
            {
                if (results.Count >= MaxPerStore) break;
                var name = hit.TryGetProperty("name", out var nEl) ? nEl.GetString()
                         : hit.TryGetProperty("product_name", out var n2) ? n2.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;

                decimal price = 0;
                foreach (var f in new[] { "price", "sale_price", "special_price", "final_price" })
                    if (hit.TryGetProperty(f, out var pEl) && pEl.ValueKind == JsonValueKind.Number) { price = pEl.GetDecimal(); break; }
                if (price <= 0) continue;

                var sku = hit.TryGetProperty("sku", out var sEl) ? sEl.GetString() : null;
                var slug = hit.TryGetProperty("url_key", out var uEl) ? uEl.GetString() : sku;

                results.Add(new ProductSearchResult
                {
                    Name       = name,
                    Description= $"From {store.Name}.",
                    Price      = price,
                    Currency   = store.Currency,
                    StoreName  = store.Name,
                    ProductUrl = string.IsNullOrWhiteSpace(slug) ? $"https://en.namshi.com/{countrySegment}" : $"https://en.namshi.com/{countrySegment}/{slug}.html",
                    ImageUrl   = hit.TryGetProperty("thumbnail_url", out var imgEl) ? imgEl.GetString() ?? string.Empty : string.Empty,
                    InStock    = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Namshi scrape failed for {Store}: {Msg}", store.Name, ex.Message);
        }
        return results;
    }

    private async Task<List<ProductSearchResult>> ScrapeSixthStreetAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        try
        {
            var client   = _httpClientFactory.CreateClient("product-search");
            var appId    = "RFLUC9QLCO";
            var apiKey   = "4fbe0eb7bfba8c23fd5f03b06e0b72c0";
            var country  = store.SearchUrlTemplate; 
            var index    = $"products_{country}_en";
            var url      = $"https://{appId}-dsn.algolia.net/1/indexes/{index}/query";

            var body = JsonSerializer.Serialize(new { query, hitsPerPage = MaxPerStore });
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
            req.Headers.Add("X-Algolia-Application-Id", appId);
            req.Headers.Add("X-Algolia-API-Key", apiKey);

            using var cts    = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSecs));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            var resp = await client.SendAsync(req, linked.Token);
            if (!resp.IsSuccessStatusCode) return results;

            var json = await resp.Content.ReadAsStringAsync(linked.Token);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.ValueKind != JsonValueKind.Array)
                return results;

            foreach (var hit in hits.EnumerateArray())
            {
                if (results.Count >= MaxPerStore) break;
                var name = hit.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;

                decimal price = 0;
                foreach (var f in new[] { "price", "sale_price", "special_price", "final_price" })
                    if (hit.TryGetProperty(f, out var pEl) && pEl.ValueKind == JsonValueKind.Number) { price = pEl.GetDecimal(); break; }
                if (price <= 0) continue;

                var sku = hit.TryGetProperty("sku", out var sEl) ? sEl.GetString() : null;
                var slug = hit.TryGetProperty("url_key", out var uEl) ? uEl.GetString() : sku;

                results.Add(new ProductSearchResult
                {
                    Name       = name,
                    Description= $"From {store.Name}.",
                    Price      = price,
                    Currency   = store.Currency,
                    StoreName  = store.Name,
                    ProductUrl = string.IsNullOrWhiteSpace(slug) ? $"https://www.6thstreet.com/{country}" : $"https://www.6thstreet.com/{country}/{slug}.html",
                    ImageUrl   = hit.TryGetProperty("thumbnail_url", out var imgEl) ? imgEl.GetString() ?? string.Empty : string.Empty,
                    InStock    = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("6thStreet scrape failed for {Store}: {Msg}", store.Name, ex.Message);
        }
        return results;
    }

    private async Task<List<ProductSearchResult>> ScrapeFarfetchAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        try
        {
            var client = _httpClientFactory.CreateClient("product-search");
            var url    = $"{store.SearchUrlTemplate}/api/catalog/search?q={Uri.EscapeDataString(query)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("Accept", "application/json");

            using var cts    = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSecs));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            var resp = await client.SendAsync(req, linked.Token);
            if (!resp.IsSuccessStatusCode) return results;

            var json = await resp.Content.ReadAsStringAsync(linked.Token);
            using var doc = JsonDocument.Parse(json);
            var products = FindArray(doc.RootElement, "products");
            if (products.ValueKind != JsonValueKind.Array) return results;

            foreach (var item in products.EnumerateArray())
            {
                if (results.Count >= MaxPerStore) break;
                var name = item.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;

                decimal price = 0;
                if (item.TryGetProperty("price", out var pEl) && pEl.ValueKind == JsonValueKind.Object)
                {
                    var amount = pEl.TryGetProperty("amount", out var aEl) ? aEl.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(amount))
                        decimal.TryParse(amount.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                }
                if (price <= 0) continue;

                var currency = item.TryGetProperty("price", out var priceObj) && priceObj.ValueKind == JsonValueKind.Object
                    ? priceObj.TryGetProperty("currency", out var curEl) ? curEl.GetString() : "USD"
                    : "USD";

                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                var imgUrl = item.TryGetProperty("images", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                    ? imgs.EnumerateArray().FirstOrDefault().TryGetProperty("url", out var imgEl) ? imgEl.GetString() : string.Empty
                    : string.Empty;

                results.Add(new ProductSearchResult
                {
                    Name       = name,
                    Description= "From Farfetch.",
                    Price      = price,
                    Currency   = currency,
                    StoreName  = "Farfetch",
                    ProductUrl = string.IsNullOrWhiteSpace(id) ? store.SearchUrlTemplate : $"{store.SearchUrlTemplate}/shopping/item-{id}.aspx",
                    ImageUrl   = imgUrl ?? string.Empty,
                    InStock    = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Farfetch scrape failed: {Msg}", ex.Message);
        }
        return results;
    }

    private async Task<List<ProductSearchResult>> ScrapeGenericHtmlAsync(StoreDescriptor store, string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        try
        {
            var url  = store.SearchUrlTemplate.Replace("{q}", Uri.EscapeDataString(query));
            var html = await FetchHtmlAsync(url, ct);
            if (string.IsNullOrWhiteSpace(html)) return results;

            var doc = _parser.ParseDocument(html);
            var productSelectors = new[]
            {
                "div.product-item", "div.product", "div.item", "article.product", "li.product",
                "[data-product]", ".product-card", ".product-tile", ".product-box"
            };

            foreach (var selector in productSelectors)
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

                    var baseUrl = new Uri(url);
                    var productUrl = href.StartsWith("http") ? href : $"{baseUrl.Scheme}://{baseUrl.Host}{href}";

                    results.Add(new ProductSearchResult
                    {
                        Name       = name,
                        Description= $"From {store.Name}.",
                        Price      = price,
                        Currency   = store.Currency,
                        StoreName  = store.Name,
                        ProductUrl = productUrl,
                        ImageUrl   = item.QuerySelector("img")?.GetAttribute("src") ?? string.Empty,
                        InStock    = true
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

    private static JsonElement FindArray(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var prop))
            return prop;
        foreach (var child in el.EnumerateObject())
        {
            var found = FindArray(child.Value, name);
            if (found.ValueKind != JsonValueKind.Undefined) return found;
        }
        return default;
    }

    private async Task<string?> SendAsync(HttpClient client, HttpRequestMessage req, CancellationToken ct)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSecs));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            var resp = await client.SendAsync(req, linked.Token);
            return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync(linked.Token) : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> FetchHtmlAsync(string url, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("product-search");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSecs));
            return await client.GetStringAsync(url, cts.Token);
        }
        catch
        {
            return null;
        }
    }

    private async Task AddLocalResultsAsync(List<ProductSearchResult> results, ProductFilterRequest filter)
    {
        try
        {
            var localPaged = await _productRepository.GetAllAsync(
                filter,
                new PaginationRequest { Page = 0, Size = 50 });

            foreach (var p in localPaged.Content)
            {
                var activeListings  = p.Listings.Where(l => l.IsActive).ToList();
                var latestPrices    = activeListings
                    .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).FirstOrDefault())
                    .Where(ph => ph != null)
                    .ToList();

                var price    = latestPrices.Any() ? latestPrices.Min(ph => ph!.Price) : 0;
                var currency = latestPrices.FirstOrDefault(ph => ph!.Price == price)?.CurrencyCode
                            ?? latestPrices.FirstOrDefault()?.CurrencyCode
                            ?? "EGP";

                if (price <= 0) continue;

                results.Add(new ProductSearchResult
                {
                    Name        = p.Name,
                    Description = p.Description ?? string.Empty,
                    ImageUrl    = p.Images.FirstOrDefault(i => i.IsPrimary)?.Url ?? p.Images.FirstOrDefault()?.Url ?? string.Empty,
                    Price       = price,
                    Currency    = currency,
                    StoreName   = "Local Catalog",
                    ProductUrl  = p.ProductId.ToString(),
                    InStock     = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch local products during search");
        }
    }

    private async Task<IEnumerable<ProductSearchResult>> ApplyFiltersAsync(
        IEnumerable<ProductSearchResult> results,
        ProductFilterRequest filter)
    {
        var filtered = results;

        if (filter.StoreId.HasValue)
        {
            var store = await _storeRepository.GetByIdAsync(filter.StoreId.Value);
            filtered = store is null
                ? []
                : filtered.Where(r =>
                    string.Equals(r.StoreName, store.Name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(r.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase));
        }

        if (filter.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(filter.CategoryId.Value);
            if (category is null)
            {
                filtered = [];
            }
            else
            {
                filtered = filtered.Where(r =>
                    string.Equals(r.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(InferCategoryName(r.Name, r.ProductUrl), category.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (filter.MinPrice.HasValue)
            filtered = filtered.Where(r => r.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            filtered = filtered.Where(r => r.Price <= filter.MaxPrice.Value);

        return filtered;
    }

    private static IEnumerable<ProductSearchResult> SortResults(IEnumerable<ProductSearchResult> results, string? sortBy)
        => sortBy switch
        {
            "price_asc"  => results.OrderBy(r => r.Price),
            "price_desc" => results.OrderByDescending(r => r.Price),
            "name"       => results.OrderBy(r => r.Name),
            _            => results
        };

    private async Task MergeExistingListingsAsync(List<ProductSearchResult> results)
    {
        try
        {
            var listings = (await _listingRepository.GetAllAsync())
                .Where(l => !string.IsNullOrWhiteSpace(l.ProductUrl))
                .GroupBy(l => NormalizeProductUrl(l.ProductUrl), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var result in results.Where(r => !string.Equals(r.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase)))
            {
                if (!listings.TryGetValue(NormalizeProductUrl(result.ProductUrl), out var listing))
                    continue;

                result.ProductUrl = listing.ProductId.ToString();
                await RecordLatestPriceAsync(listing, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to merge live search results with existing listings");
        }
    }

    private async Task RecordLatestPriceAsync(StoreProductListing listing, ProductSearchResult result)
    {
        var currencyCode = string.IsNullOrWhiteSpace(result.Currency) ? "USD" : result.Currency.Trim().ToUpperInvariant();
        var latest = listing.PriceHistories
            .OrderByDescending(ph => ph.RecordedAt)
            .FirstOrDefault();

        if (latest != null
            && latest.CurrencyCode == currencyCode
            && Math.Abs(latest.Price - result.Price) < 0.01m
            && latest.RecordedAt > DateTime.UtcNow.AddMinutes(-10))
        {
            return;
        }

        await _priceHistoryRepository.AddAsync(new PriceHistory
        {
            ListingId    = listing.ListingId,
            Price        = result.Price,
            CurrencyCode = currencyCode,
            RecordedAt   = DateTime.UtcNow,
            ScrapedAt    = DateTime.UtcNow
        });

        listing.LastScrapedAt = DateTime.UtcNow;
        await _listingRepository.UpdateAsync(listing);
    }

    private async Task<ProductSearchResult?> ScrapeProductDetailsAsync(string url)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("product-search");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var html = await client.GetStringAsync(url, cts.Token);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var parser = new HtmlParser();
            var doc    = parser.ParseDocument(html);
            var structured = ExtractStructuredProduct(
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

            var price    = structured?.Price ?? 0;
            var priceStr = price > 0 ? null
                : doc.QuerySelector("meta[property='product:price:amount']")?.GetAttribute("content")
                ?? doc.QuerySelector("meta[property='og:price:amount']")?.GetAttribute("content")
                ?? doc.QuerySelector("[data-price]")?.GetAttribute("data-price")
                ?? doc.QuerySelector("[itemprop='price']")?.GetAttribute("content")
                ?? doc.QuerySelector("[itemprop='price']")?.TextContent
                ?? doc.QuerySelector(".a-price .a-offscreen")?.TextContent
                ?? doc.QuerySelector(".price")?.TextContent
                ?? doc.QuerySelector(".prc")?.TextContent;

            if (!string.IsNullOrWhiteSpace(priceStr))
            {
                var match = Regex.Match(priceStr, @"(\d{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?|\d+(?:\.\d{1,2})?)");
                if (match.Success)
                    decimal.TryParse(match.Groups[1].Value.Replace(",", "").Replace(" ", ""),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out price);
            }

            if (string.IsNullOrWhiteSpace(name) || price <= 0)
                return null;

            return new ProductSearchResult
            {
                Name        = name,
                Description = description,
                Price       = price,
                Currency    = structured?.Currency
                    ?? doc.QuerySelector("meta[property='product:price:currency']")?.GetAttribute("content")
                    ?? doc.QuerySelector("meta[property='og:price:currency']")?.GetAttribute("content")
                    ?? InferCurrencyCode(url),
                StoreName   = InferStoreName(url),
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

    private static void CacheSearchResults(IEnumerable<ProductSearchResult> results)
    {
        var expiresAt = DateTime.UtcNow.Add(SearchResultCacheLifetime);
        foreach (var r in results.Where(r => !string.IsNullOrWhiteSpace(r.ProductUrl) && r.Price > 0))
            RecentSearchResults[NormalizeProductUrl(r.ProductUrl)] = new CachedSearchResult(r, expiresAt);
    }

    private static string NormalizeProductUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim();
        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private static ProductSearchResult CloneSearchResult(ProductSearchResult r, string productUrl)
        => new()
        {
            Name        = r.Name,
            Description = r.Description,
            ImageUrl    = r.ImageUrl,
            Price       = r.Price,
            Currency    = r.Currency,
            StoreName   = r.StoreName,
            ProductUrl  = productUrl,
            VariantInfo = r.VariantInfo,
            Rating      = r.Rating,
            ReviewCount = r.ReviewCount,
            InStock     = r.InStock
        };

    private static string BuildBaseUrl(string storeName)
    {
        var slug = storeName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace(".", "")
            .Replace("'", "");
        return $"https://www.{slug}.com";
    }

    private static string InferStoreName(string url)
    {
        if (url.Contains("amazon.eg",   StringComparison.OrdinalIgnoreCase)) return "Amazon Egypt";
        if (url.Contains("amazon.sa",   StringComparison.OrdinalIgnoreCase)) return "Amazon Saudi Arabia";
        if (url.Contains("amazon.ae",   StringComparison.OrdinalIgnoreCase)) return "Amazon UAE";
        if (url.Contains("noon.com/egypt",  StringComparison.OrdinalIgnoreCase)) return "Noon Egypt";
        if (url.Contains("noon.com/saudi",  StringComparison.OrdinalIgnoreCase)) return "Noon Saudi Arabia";
        if (url.Contains("noon.com/uae",    StringComparison.OrdinalIgnoreCase)) return "Noon UAE";
        if (url.Contains("jumia.com.eg",    StringComparison.OrdinalIgnoreCase)) return "Jumia Egypt";
        if (url.Contains("jumia.com.sa",    StringComparison.OrdinalIgnoreCase)) return "Jumia Saudi Arabia";
        if (url.Contains("temu.com",        StringComparison.OrdinalIgnoreCase)) return "Temu";
        if (url.Contains("aliexpress.com",  StringComparison.OrdinalIgnoreCase)) return "AliExpress";
        if (url.Contains("ebay.com",        StringComparison.OrdinalIgnoreCase)) return "eBay";
        if (url.Contains("namshi.com",      StringComparison.OrdinalIgnoreCase)) return "Namshi";
        if (url.Contains("6thstreet.com",   StringComparison.OrdinalIgnoreCase)) return "6thStreet";
        if (url.Contains("farfetch.com",    StringComparison.OrdinalIgnoreCase)) return "Farfetch";
        return "Online Store";
    }

    private static string InferCurrencyCode(string url)
    {
        if (url.Contains("amazon.sa",    StringComparison.OrdinalIgnoreCase)
         || url.Contains("noon.com/saudi", StringComparison.OrdinalIgnoreCase)) return "SAR";
        if (url.Contains("amazon.ae",    StringComparison.OrdinalIgnoreCase)
         || url.Contains("noon.com/uae",   StringComparison.OrdinalIgnoreCase)) return "AED";
        return "EGP";
    }

    private static string InferCategoryName(string name, string url)
    {
        var h = $"{name} {url}".ToLowerInvariant();
        if (ContainsAny(h, "iphone", "samsung galaxy", "smartphone", "mobile phone")) return "Mobile Phones";
        if (ContainsAny(h, "laptop", "notebook", "macbook", "thinkpad"))             return "Laptops";
        if (ContainsAny(h, "tablet", "ipad"))                                         return "Tablets";
        if (ContainsAny(h, "headphone", "earbud", "airpods", "speaker"))             return "Audio";
        if (ContainsAny(h, "tv", "television", "monitor", "display"))                return "TVs & Monitors";
        if (ContainsAny(h, "watch", "smartwatch"))                                   return "Wearables";
        if (ContainsAny(h, "shoe", "shirt", "dress", "jeans", "jacket", "fashion")) return "Fashion";
        if (ContainsAny(h, "fridge", "refrigerator", "washer", "microwave"))        return "Home Appliances";
        if (ContainsAny(h, "sofa", "chair", "table", "bed", "furniture"))           return "Furniture";
        if (ContainsAny(h, "makeup", "perfume", "skincare", "beauty"))              return "Beauty";
        return "General";
    }

    private static bool ContainsAny(string value, params string[] needles)
        => needles.Any(value.Contains);

    private static StructuredProduct? ExtractStructuredProduct(IEnumerable<string> scriptBodies)
    {
        foreach (var body in scriptBodies.Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            try
            {
                using var json = JsonDocument.Parse(body);
                var product    = FindProductObject(json.RootElement);
                if (product.HasValue) return MapStructuredProduct(product.Value);
            }
            catch (JsonException) { continue; }
        }
        return null;
    }

    private static JsonElement? FindProductObject(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            if (IsProductType(el)) return el;
            foreach (var prop in el.EnumerateObject())
            {
                var n = FindProductObject(prop.Value);
                if (n.HasValue) return n;
            }
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                var n = FindProductObject(item);
                if (n.HasValue) return n;
            }
        }
        return null;
    }

    private static bool IsProductType(JsonElement el)
    {
        if (!el.TryGetProperty("@type", out var t)) return false;
        return t.ValueKind switch
        {
            JsonValueKind.String => string.Equals(t.GetString(), "Product", StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Array  => t.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String
                && string.Equals(x.GetString(), "Product", StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static StructuredProduct MapStructuredProduct(JsonElement product)
    {
        var offer    = product.TryGetProperty("offers", out var offers) ? FirstObject(offers) : null;
        var priceStr = offer.HasValue ? GetString(offer.Value, "price", "lowPrice", "highPrice") : null;
        decimal.TryParse(priceStr?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var price);
        return new StructuredProduct(
            GetString(product, "name"),
            GetString(product, "description"),
            GetImage(product),
            price,
            offer.HasValue ? GetString(offer.Value, "priceCurrency") : null);
    }

    private static JsonElement? FirstObject(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Object) return el;
        if (el.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in el.EnumerateArray())
            if (item.ValueKind == JsonValueKind.Object) return item;
        return null;
    }

    private static string? GetString(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!el.TryGetProperty(name, out var val)) continue;
            if (val.ValueKind == JsonValueKind.String) return val.GetString()?.Trim();
            if (val.ValueKind == JsonValueKind.Number) return val.GetDecimal().ToString();
        }
        return null;
    }

    private static string? GetImage(JsonElement el)
    {
        if (!el.TryGetProperty("image", out var image)) return null;
        if (image.ValueKind == JsonValueKind.String) return image.GetString()?.Trim();
        if (image.ValueKind == JsonValueKind.Array)
            return image.EnumerateArray()
                .Select(i => i.ValueKind == JsonValueKind.String ? i.GetString()?.Trim() : null)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        return image.ValueKind == JsonValueKind.Object ? GetString(image, "url", "contentUrl") : null;
    }

    private sealed record CachedSearchResult(ProductSearchResult Result, DateTime ExpiresAt);
    private sealed record StructuredProduct(string? Name, string? Description, string? ImageUrl, decimal Price, string? Currency);
}