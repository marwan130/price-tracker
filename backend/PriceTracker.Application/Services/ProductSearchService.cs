namespace PriceTracker.Application.Services;

using Microsoft.Extensions.Logging;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;

public class ProductSearchService : IProductSearchService
{
    private static readonly ConcurrentDictionary<string, CachedSearchResult> RecentSearchResults = new();
    private static readonly TimeSpan SearchResultCacheLifetime = TimeSpan.FromMinutes(30);
    private const int SearchPagesPerStore = 3;
    private const int MaxResultsPerStore = 36;

    private readonly ILogger<ProductSearchService> _logger;
    private readonly IProductRepository _productRepository;
    private readonly IScrapeLogRepository _scrapeLogRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IListingRepository _listingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductSearchService(
        ILogger<ProductSearchService> logger,
        IProductRepository productRepository,
        IScrapeLogRepository scrapeLogRepository,
        IStoreRepository storeRepository,
        IListingRepository listingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _productRepository = productRepository;
        _scrapeLogRepository = scrapeLogRepository;
        _storeRepository = storeRepository;
        _listingRepository = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(string query, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching for products: {Query}", query);

        if (string.IsNullOrWhiteSpace(query))
            return [];

        var results = new List<ProductSearchResult>();

        // 1. Search local database products matching the query
        try
        {
            var localPaged = await _productRepository.GetAllAsync(
                new ProductFilterRequest { Query = query },
                new PaginationRequest { Page = 0, Size = 10 }
            );

            foreach (var p in localPaged.Content)
            {
                var activeListings = p.Listings.Where(l => l.IsActive).ToList();
                var latestPrices = activeListings
                    .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).FirstOrDefault())
                    .Where(ph => ph != null)
                    .ToList();

                decimal price = latestPrices.Any() ? latestPrices.Min(ph => ph!.Price) : 0;
                string currency = latestPrices.FirstOrDefault(ph => ph!.Price == price)?.CurrencyCode 
                                   ?? latestPrices.FirstOrDefault()?.CurrencyCode ?? "EGP";

                results.Add(new ProductSearchResult
                {
                    Name = p.Name,
                    Description = p.Description ?? string.Empty,
                    ImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.Url ?? p.Images.FirstOrDefault()?.Url ?? string.Empty,
                    Price = price,
                    Currency = currency,
                    StoreName = "Local Catalog",
                    ProductUrl = p.ProductId.ToString(),
                    InStock = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch local products during search");
        }

        // 2. Perform live scraping search of external stores in the MENA region (Egypt, KSA, UAE)
        var liveSearchStartedAt = DateTime.UtcNow;
        var tasks = new List<Task<List<ProductSearchResult>>>
        {
            ScrapeAmazonSearchAsync("amazon.eg", "Amazon Egypt", "EGP", query, ct),
            ScrapeAmazonSearchAsync("amazon.sa", "Amazon Saudi Arabia", "SAR", query, ct),
            ScrapeAmazonSearchAsync("amazon.ae", "Amazon UAE", "AED", query, ct),
            ScrapeNoonSearchAsync("egypt-en", "Noon Egypt", "EGP", query, ct),
            ScrapeNoonSearchAsync("saudi-en", "Noon Saudi Arabia", "SAR", query, ct),
            ScrapeNoonSearchAsync("uae-en", "Noon UAE", "AED", query, ct),
            ScrapeJumiaSearchAsync(query, ct)
        };

        try
        {
            await Task.WhenAll(tasks);
            foreach (var task in tasks)
            {
                results.AddRange(task.Result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "One or more live store scrapers failed");
        }

        await MergeExistingListingsAsync(results);

        results = results
            .Where(result => result.Price > 0 && !string.IsNullOrWhiteSpace(result.Name))
            .GroupBy(result => NormalizeProductUrl(result.ProductUrl), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(result => string.Equals(result.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase)).First())
            .ToList();

        CacheSearchResults(results);
        await LogLiveSearchAsync(query, results, liveSearchStartedAt);
        return results;
    }

    private async Task LogLiveSearchAsync(string query, IEnumerable<ProductSearchResult> results, DateTime startedAt)
    {
        try
        {
            var finishedAt = DateTime.UtcNow;
            var stores = (await _storeRepository.GetAllAsync()).ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
            var liveStoreNames = new[]
            {
                "Amazon Egypt",
                "Amazon Saudi Arabia",
                "Amazon UAE",
                "Noon Egypt",
                "Noon Saudi Arabia",
                "Noon UAE",
                "Jumia"
            };
            var counts = results
                .Where(result => !string.Equals(result.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase))
                .GroupBy(result => result.StoreName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var storeName in liveStoreNames)
            {
                if (!stores.TryGetValue(storeName, out var store))
                    continue;

                counts.TryGetValue(storeName, out var count);
                await _scrapeLogRepository.AddAsync(new ScrapeLog
                {
                    StoreId = store.StoreId,
                    Status = count > 0 ? ScrapeStatus.Success : ScrapeStatus.Partial,
                    ErrorMessage = count > 0 ? null : $"Live search returned no results for '{query}'.",
                    ItemsScraped = count,
                    StartedAt = startedAt,
                    FinishedAt = finishedAt
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record live search scrape logs for {Query}", query);
        }
    }

    private async Task MergeExistingListingsAsync(List<ProductSearchResult> results)
    {
        try
        {
            var listings = (await _listingRepository.GetAllAsync())
                .Where(listing => !string.IsNullOrWhiteSpace(listing.ProductUrl))
                .GroupBy(listing => NormalizeProductUrl(listing.ProductUrl), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var result in results.Where(result => !string.Equals(result.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase)))
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
            ListingId = listing.ListingId,
            Price = result.Price,
            CurrencyCode = currencyCode,
            RecordedAt = DateTime.UtcNow,
            ScrapedAt = DateTime.UtcNow
        });

        listing.LastScrapedAt = DateTime.UtcNow;
        await _listingRepository.UpdateAsync(listing);
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

    private static void CacheSearchResults(IEnumerable<ProductSearchResult> results)
    {
        var expiresAt = DateTime.UtcNow.Add(SearchResultCacheLifetime);
        foreach (var result in results.Where(r => !string.IsNullOrWhiteSpace(r.ProductUrl) && r.Price > 0))
        {
            RecentSearchResults[NormalizeProductUrl(result.ProductUrl)] = new CachedSearchResult(result, expiresAt);
        }
    }

    private static string NormalizeProductUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim();

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private static ProductSearchResult CloneSearchResult(ProductSearchResult result, string productUrl)
        => new()
        {
            Name = result.Name,
            Description = result.Description,
            ImageUrl = result.ImageUrl,
            Price = result.Price,
            Currency = result.Currency,
            StoreName = result.StoreName,
            ProductUrl = productUrl,
            VariantInfo = result.VariantInfo,
            Rating = result.Rating,
            ReviewCount = result.ReviewCount,
            InStock = result.InStock
        };

    private sealed record CachedSearchResult(ProductSearchResult Result, DateTime ExpiresAt);

    private async Task<string?> FetchHtmlAsync(string url, int timeoutSeconds = 15)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("product-search");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            return await client.GetStringAsync(url, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to fetch HTML from remote endpoint: {Message}", ex.Message);
            return null;
        }
    }

    private async Task<List<ProductSearchResult>> ScrapeAmazonSearchAsync(
        string domain,
        string storeName,
        string currency,
        string query,
        CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        try
        {
            var parser = new HtmlParser();
            for (var page = 1; page <= SearchPagesPerStore && results.Count < MaxResultsPerStore; page++)
            {
                var url = $"https://www.{domain}/s?k={Uri.EscapeDataString(query)}&page={page}";
                var html = await FetchHtmlAsync(url, timeoutSeconds: 6);
                if (string.IsNullOrWhiteSpace(html)) break;

                var beforeCount = results.Count;
                var doc = parser.ParseDocument(html);
                var items = doc.QuerySelectorAll("div[data-component-type='s-search-result']");
                foreach (var item in items)
                {
                    if (results.Count >= MaxResultsPerStore) break;

                    var nameEl = item.QuerySelector("h2 a span");
                    var name = nameEl?.TextContent?.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var linkEl = item.QuerySelector("h2 a");
                    var href = linkEl?.GetAttribute("href");
                    if (string.IsNullOrEmpty(href)) continue;
                    var productUrl = href.StartsWith("http") ? href : $"https://www.{domain}{href}";

                    var qIdx = productUrl.IndexOf('?');
                    if (qIdx > 0) productUrl = productUrl[..qIdx];

                    var imgEl = item.QuerySelector("img.s-image");
                    var imageUrl = imgEl?.GetAttribute("src") ?? string.Empty;

                    var priceWhole = item.QuerySelector(".a-price-whole")?.TextContent?.Trim().Replace(",", "");
                    var priceFraction = item.QuerySelector(".a-price-fraction")?.TextContent?.Trim();

                    decimal price = 0;
                    if (!string.IsNullOrEmpty(priceWhole))
                    {
                        var priceStr = priceWhole;
                        if (!string.IsNullOrEmpty(priceFraction)) priceStr += "." + priceFraction;
                        decimal.TryParse(priceStr, out price);
                    }
                    if (price <= 0) continue;

                    results.Add(new ProductSearchResult
                    {
                        Name = name,
                        Description = $"Compare prices for {name} on {storeName}.",
                        Price = price,
                        Currency = currency,
                        StoreName = storeName,
                        ProductUrl = productUrl,
                        ImageUrl = imageUrl,
                        InStock = true
                    });
                }

                if (results.Count == beforeCount) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search scraping operation failed for store");
        }
        return results;
    }

    private async Task<List<ProductSearchResult>> ScrapeJumiaSearchAsync(string query, CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        try
        {
            var parser = new HtmlParser();
            for (var page = 1; page <= SearchPagesPerStore && results.Count < MaxResultsPerStore; page++)
            {
                var url = $"https://www.jumia.com.eg/catalog/?q={Uri.EscapeDataString(query)}&page={page}";
                var html = await FetchHtmlAsync(url, timeoutSeconds: 6);
                if (string.IsNullOrWhiteSpace(html)) break;

                var beforeCount = results.Count;
                var doc = parser.ParseDocument(html);
                var items = doc.QuerySelectorAll("article.prd");
                foreach (var item in items)
                {
                    if (results.Count >= MaxResultsPerStore) break;

                    var nameEl = item.QuerySelector(".name");
                    var name = nameEl?.TextContent?.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var linkEl = item.QuerySelector("a.core");
                    var href = linkEl?.GetAttribute("href");
                    if (string.IsNullOrEmpty(href)) continue;
                    var productUrl = href.StartsWith("http") ? href : $"https://www.jumia.com.eg{href}";

                    var imgEl = item.QuerySelector("img.img");
                    var imageUrl = imgEl?.GetAttribute("data-src") ?? imgEl?.GetAttribute("src") ?? string.Empty;

                    var priceEl = item.QuerySelector(".prc");
                    decimal price = 0;
                    if (priceEl != null)
                    {
                        var priceText = priceEl.TextContent;
                        var match = Regex.Match(priceText, @"[\d,]+");
                        if (match.Success)
                        {
                            decimal.TryParse(match.Value.Replace(",", ""), out price);
                        }
                    }
                    if (price <= 0) continue;

                    results.Add(new ProductSearchResult
                    {
                        Name = name,
                        Description = $"Compare prices for {name} on Jumia.",
                        Price = price,
                        Currency = "EGP",
                        StoreName = "Jumia",
                        ProductUrl = productUrl,
                        ImageUrl = imageUrl,
                        InStock = true
                    });
                }

                if (results.Count == beforeCount) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Jumia search scraping failed");
        }
        return results;
    }

    private async Task<List<ProductSearchResult>> ScrapeNoonSearchAsync(
        string pathSegment,
        string storeName,
        string currency,
        string query,
        CancellationToken ct)
    {
        var results = new List<ProductSearchResult>();
        try
        {
            for (var page = 1; page <= SearchPagesPerStore && results.Count < MaxResultsPerStore; page++)
            {
                var url = $"https://www.noon.com/{pathSegment}/search/?q={Uri.EscapeDataString(query)}&page={page}";
                var html = await FetchHtmlAsync(url, timeoutSeconds: 6);
                if (string.IsNullOrWhiteSpace(html) || !html.Contains("__NEXT_DATA__")) break;

                var match = Regex.Match(html, @"<script id=""__NEXT_DATA__"" type=""application/json"">(.*?)</script>");
                if (!match.Success) break;

                var beforeCount = results.Count;
                using var doc = JsonDocument.Parse(match.Groups[1].Value);
                if (doc.RootElement.TryGetProperty("props", out var props) &&
                    props.TryGetProperty("pageProps", out var pageProps) &&
                    pageProps.TryGetProperty("catalog", out var catalog) &&
                    catalog.TryGetProperty("hits", out var hits) &&
                    hits.ValueKind == JsonValueKind.Array)
                {
                    foreach (var hit in hits.EnumerateArray())
                    {
                        if (results.Count >= MaxResultsPerStore) break;

                        var name = hit.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                        if (string.IsNullOrEmpty(name)) continue;

                        var sku = hit.TryGetProperty("product_sku", out var skuEl) ? skuEl.GetString() : string.Empty;
                        var productUrl = $"https://www.noon.com/{pathSegment}/{sku}/p/";

                        var priceEl = hit.TryGetProperty("price", out var pEl) ? pEl.GetDecimal() : 0;
                        if (priceEl <= 0) continue;
                        var imgUrl = hit.TryGetProperty("image_key", out var imgEl) ? $"https://f.nooncdn.com/p/{imgEl.GetString()}.jpg" : string.Empty;

                        results.Add(new ProductSearchResult
                        {
                            Name = name,
                            Description = $"Compare prices for {name} on {storeName}.",
                            Price = priceEl,
                            Currency = currency,
                            StoreName = storeName,
                            ProductUrl = productUrl,
                            ImageUrl = imgUrl,
                            InStock = true
                        });
                    }
                }

                if (results.Count == beforeCount) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search scraping operation failed for store");
        }

        return results;
    }

    private async Task<ProductSearchResult?> ScrapeProductDetailsAsync(string url)
    {
        try
        {
            var html = await FetchHtmlAsync(url);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(html);

            string storeName = "Online Store";
            if (url.Contains("amazon")) storeName = "Amazon Egypt";
            else if (url.Contains("noon")) storeName = "Noon";
            else if (url.Contains("jumia")) storeName = "Jumia";

            var structured = ExtractStructuredProduct(doc.QuerySelectorAll("script[type='application/ld+json']")
                .Select(script => script.TextContent));

            var name = structured?.Name
                    ?? doc.QuerySelector("h1")?.TextContent?.Trim()
                    ?? doc.QuerySelector("meta[property='og:title']")?.GetAttribute("content")?.Trim();
            
            if (string.IsNullOrEmpty(name))
            {
                var uri = new Uri(url);
                var lastSegment = uri.Segments.LastOrDefault()?.Trim('/');
                name = string.IsNullOrEmpty(lastSegment) ? null : lastSegment.Replace("-", " ");
            }

            var description = structured?.Description
                           ?? doc.QuerySelector("meta[property='og:description']")?.GetAttribute("content")?.Trim()
                           ?? doc.QuerySelector("meta[name='description']")?.GetAttribute("content")?.Trim()
                           ?? $"Automatically imported product from {storeName}.";

            var imageUrl = structured?.ImageUrl
                        ?? doc.QuerySelector("meta[property='og:image']")?.GetAttribute("content")?.Trim()
                        ?? doc.QuerySelector("img")?.GetAttribute("src")?.Trim()
                        ?? string.Empty;

            decimal price = structured?.Price ?? 0;
            var priceStr = price > 0 ? null : doc.QuerySelector("meta[property='product:price:amount']")?.GetAttribute("content")
                        ?? doc.QuerySelector("meta[property='og:price:amount']")?.GetAttribute("content")
                        ?? doc.QuerySelector("[data-price]")?.GetAttribute("data-price")
                        ?? doc.QuerySelector("[itemprop='price']")?.GetAttribute("content")
                        ?? doc.QuerySelector("[itemprop='price']")?.TextContent
                        ?? doc.QuerySelector(".a-price .a-offscreen")?.TextContent
                        ?? doc.QuerySelector(".price")?.TextContent
                        ?? doc.QuerySelector(".-b.-ltr")?.TextContent
                        ?? doc.QuerySelector(".prc")?.TextContent;
            
            if (!string.IsNullOrEmpty(priceStr))
            {
                var match = Regex.Match(priceStr, @"(\d{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?|\d+(?:\.\d{1,2})?)");
                if (match.Success)
                    decimal.TryParse(match.Groups[1].Value.Replace(",", "").Replace(" ", ""), out price);
            }

            if (string.IsNullOrWhiteSpace(name) || price <= 0)
                return null;

            var currency = structured?.Currency
                        ?? doc.QuerySelector("meta[property='product:price:currency']")?.GetAttribute("content")
                        ?? doc.QuerySelector("meta[property='og:price:currency']")?.GetAttribute("content")
                        ?? "EGP";

            return new ProductSearchResult
            {
                Name = name,
                Description = description,
                Price = price,
                Currency = currency,
                StoreName = storeName,
                ProductUrl = url,
                ImageUrl = imageUrl,
                InStock = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to scrape product details: {Message}", ex.Message);
            return null;
        }
    }

    private static StructuredProduct? ExtractStructuredProduct(IEnumerable<string> scriptBodies)
    {
        foreach (var body in scriptBodies.Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            try
            {
                using var json = JsonDocument.Parse(body);
                var product = FindProductObject(json.RootElement);
                if (product.HasValue)
                    return MapStructuredProduct(product.Value);
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return null;
    }

    private static JsonElement? FindProductObject(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (IsProductType(element))
                return element;

            if (element.TryGetProperty("@graph", out var graph))
            {
                var fromGraph = FindProductObject(graph);
                if (fromGraph.HasValue)
                    return fromGraph;
            }

            foreach (var property in element.EnumerateObject())
            {
                var nested = FindProductObject(property.Value);
                if (nested.HasValue)
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindProductObject(item);
                if (nested.HasValue)
                    return nested;
            }
        }

        return null;
    }

    private static bool IsProductType(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type))
            return false;

        return type.ValueKind switch
        {
            JsonValueKind.String => string.Equals(type.GetString(), "Product", StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Array => type.EnumerateArray().Any(t =>
                t.ValueKind == JsonValueKind.String &&
                string.Equals(t.GetString(), "Product", StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static StructuredProduct MapStructuredProduct(JsonElement product)
    {
        var offer = product.TryGetProperty("offers", out var offers)
            ? FirstObject(offers)
            : null;

        var priceText = offer.HasValue ? GetString(offer.Value, "price", "lowPrice", "highPrice") : null;
        decimal.TryParse(priceText?.Replace(",", ""), out var price);

        return new StructuredProduct(
            GetString(product, "name"),
            GetString(product, "description"),
            GetImage(product),
            price,
            offer.HasValue ? GetString(offer.Value, "priceCurrency") : null);
    }

    private static JsonElement? FirstObject(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
            return element;

        if (element.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
                return item;
        }

        return null;
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.String)
                return value.GetString()?.Trim();

            if (value.ValueKind == JsonValueKind.Number)
                return value.GetDecimal().ToString();
        }

        return null;
    }

    private static string? GetImage(JsonElement element)
    {
        if (!element.TryGetProperty("image", out var image))
            return null;

        if (image.ValueKind == JsonValueKind.String)
            return image.GetString()?.Trim();

        if (image.ValueKind == JsonValueKind.Array)
            return image.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString()?.Trim() : null)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return image.ValueKind == JsonValueKind.Object ? GetString(image, "url", "contentUrl") : null;
    }

    private sealed record StructuredProduct(
        string? Name,
        string? Description,
        string? ImageUrl,
        decimal Price,
        string? Currency);
}
