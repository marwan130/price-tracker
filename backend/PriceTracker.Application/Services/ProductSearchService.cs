namespace PriceTracker.Application.Services;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Microsoft.Extensions.Logging;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Application.Scrapers;
using PriceTracker.Domain.Entities;

public class ProductSearchService : IProductSearchService
{
    private static readonly ConcurrentDictionary<string, CachedResult> LocalCache
        = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan LocalCacheTtl  = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RedisCacheTtl  = TimeSpan.FromMinutes(5);

    private readonly ILogger<ProductSearchService>  _logger;
    private readonly IProductRepository             _productRepository;
    private readonly ICategoryRepository            _categoryRepository;
    private readonly IStoreRepository               _storeRepository;
    private readonly IListingRepository             _listingRepository;
    private readonly IPriceHistoryRepository        _priceHistoryRepository;
    private readonly IBackgroundJobClient           _backgroundJobClient;
    private readonly IEnumerable<ISearchScraper>   _scrapers;
    private readonly IProductDetailScraper          _detailScraper;
    private readonly ICacheService?                 _cache;

    public ProductSearchService(
        ILogger<ProductSearchService>  logger,
        IProductRepository             productRepository,
        ICategoryRepository            categoryRepository,
        IStoreRepository               storeRepository,
        IListingRepository             listingRepository,
        IPriceHistoryRepository        priceHistoryRepository,
        IBackgroundJobClient           backgroundJobClient,
        IEnumerable<ISearchScraper>    scrapers,
        IProductDetailScraper          detailScraper,
        ICacheService?                 cache = null)
    {
        _logger                 = logger;
        _productRepository      = productRepository;
        _categoryRepository     = categoryRepository;
        _storeRepository        = storeRepository;
        _listingRepository      = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _backgroundJobClient    = backgroundJobClient;
        _scrapers               = scrapers;
        _detailScraper          = detailScraper;
        _cache                  = cache;
    }

    public async Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(
        ProductFilterRequest filter, CancellationToken ct = default)
    {
        var query = filter.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return [];

        _logger.LogInformation("Searching for products: {Query}", query);

        var cacheKey = BuildQueryCacheKey(query, filter);
        if (_cache is not null)
        {
            var cached = await _cache.GetAsync<List<ProductSearchResult>>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogInformation("Cache hit for query '{Query}'", query);
                return SortResults(cached, filter.SortBy);
            }
        }

        var liveResults = await ScrapeAllAsync(query, ct);
        _logger.LogInformation("Live scrape returned {Count} results for '{Query}'", liveResults.Count, query);

        _backgroundJobClient.Enqueue(() => BackgroundScrapeAsync(query));

        await MergeExistingListingsAsync(liveResults);

        var filtered = Deduplicate(await ApplyFiltersAsync(liveResults, filter));

        if (filtered.Count == 0)
        {
            var local = new List<ProductSearchResult>();
            await AddLocalResultsAsync(local, filter);
            filtered = Deduplicate(await ApplyFiltersAsync(local, filter));
        }

        if (_cache is not null && filtered.Count > 0)
            await _cache.SetAsync(cacheKey, filtered, RedisCacheTtl, ct);

        UpdateLocalCache(filtered);
        return SortResults(filtered, filter.SortBy);
    }

    public async Task<ProductSearchResult?> SearchByUrlAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        _logger.LogInformation("Searching by URL: {Url}", url);

        var normalised = ScraperHelpers.NormalizeProductUrl(url);
        var urlKey     = $"search:url:{normalised}";

        if (_cache is not null)
        {
            var hit = await _cache.GetAsync<ProductSearchResult>(urlKey, ct);
            if (hit is not null) return CloneWithUrl(hit, url);
        }

        if (LocalCache.TryGetValue(normalised, out var local) && local.ExpiresAt > DateTime.UtcNow)
            return CloneWithUrl(local.Result, url);
        LocalCache.TryRemove(normalised, out _);

        var result = await _detailScraper.ScrapeAsync(url, ct);
        if (result is not null)
        {
            if (_cache is not null)
                await _cache.SetAsync(urlKey, result, LocalCacheTtl, ct);
            LocalCache[normalised] = new CachedResult(result, DateTime.UtcNow.Add(LocalCacheTtl));
        }
        return result;
    }

    public async Task BackgroundScrapeAsync(string query)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var results   = await ScrapeAllAsync(query, cts.Token);
        await MergeExistingListingsAsync(results);
        UpdateLocalCache(results);
    }

    private Task<List<ProductSearchResult>> ScrapeAllAsync(string query, CancellationToken ct)
        => Task.WhenAll(
                _scrapers.SelectMany(s => s.Stores.Select(store => s.ScrapeAsync(store, query, ct))))
            .ContinueWith(t => t.Result.SelectMany(r => r).ToList(), ct);

    private async Task AddLocalResultsAsync(List<ProductSearchResult> results, ProductFilterRequest filter)
    {
        try
        {
            var paged = await _productRepository.GetAllAsync(filter, new PaginationRequest { Page = 0, Size = 50 });

            foreach (var p in paged.Content)
            {
                var prices = p.Listings
                    .Where(l => l.IsActive)
                    .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).FirstOrDefault())
                    .Where(ph => ph != null)
                    .ToList();

                var price    = prices.Any() ? prices.Min(ph => ph!.Price) : 0;
                var currency = prices.FirstOrDefault(ph => ph!.Price == price)?.CurrencyCode
                            ?? prices.FirstOrDefault()?.CurrencyCode ?? "EGP";
                if (price <= 0) continue;

                results.Add(new ProductSearchResult
                {
                    Name        = p.Name,
                    Description = p.Description ?? string.Empty,
                    ImageUrl    = p.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                               ?? p.Images.FirstOrDefault()?.Url ?? string.Empty,
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

    private async Task<List<ProductSearchResult>> ApplyFiltersAsync(
        IEnumerable<ProductSearchResult> results, ProductFilterRequest filter)
    {
        var q = results.Where(r => r.Price > 0 && !string.IsNullOrWhiteSpace(r.Name));

        if (filter.StoreId.HasValue)
        {
            var store = await _storeRepository.GetByIdAsync(filter.StoreId.Value);
            q = store is null ? []
              : q.Where(r => string.Equals(r.StoreName, store.Name, StringComparison.OrdinalIgnoreCase)
                          || string.Equals(r.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase));
        }

        if (filter.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(filter.CategoryId.Value);
            q = category is null ? []
              : q.Where(r => string.Equals(r.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(ScraperHelpers.InferCategoryName(r.Name, r.ProductUrl),
                                 category.Name, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.MinPrice.HasValue) q = q.Where(r => r.Price >= filter.MinPrice.Value);
        if (filter.MaxPrice.HasValue) q = q.Where(r => r.Price <= filter.MaxPrice.Value);

        return q.ToList();
    }

    private async Task MergeExistingListingsAsync(List<ProductSearchResult> results)
    {
        try
        {
            var listings = (await _listingRepository.GetAllAsync())
                .Where(l => !string.IsNullOrWhiteSpace(l.ProductUrl))
                .GroupBy(l => ScraperHelpers.NormalizeProductUrl(l.ProductUrl), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var result in results.Where(r => !string.Equals(r.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase)))
            {
                if (!listings.TryGetValue(ScraperHelpers.NormalizeProductUrl(result.ProductUrl), out var listing)) continue;
                result.ProductUrl = listing.ProductId.ToString();
                await RecordLatestPriceAsync(listing, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to merge live results with existing listings");
        }
    }

    private async Task RecordLatestPriceAsync(StoreProductListing listing, ProductSearchResult result)
    {
        var currency = string.IsNullOrWhiteSpace(result.Currency) ? "USD" : result.Currency.Trim().ToUpperInvariant();
        var latest   = listing.PriceHistories.OrderByDescending(ph => ph.RecordedAt).FirstOrDefault();

        if (latest is { }
         && latest.CurrencyCode == currency
         && Math.Abs(latest.Price - result.Price) < 0.01m
         && latest.RecordedAt > DateTime.UtcNow.AddMinutes(-10))
            return;

        await _priceHistoryRepository.AddAsync(new PriceHistory
        {
            ListingId    = listing.ListingId,
            Price        = result.Price,
            CurrencyCode = currency,
            RecordedAt   = DateTime.UtcNow,
            ScrapedAt    = DateTime.UtcNow
        });

        listing.LastScrapedAt = DateTime.UtcNow;
        await _listingRepository.UpdateAsync(listing);
    }

    private static List<ProductSearchResult> Deduplicate(IEnumerable<ProductSearchResult> results)
        => results
            .GroupBy(r => ScraperHelpers.NormalizeProductUrl(r.ProductUrl), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

    private static IEnumerable<ProductSearchResult> SortResults(IEnumerable<ProductSearchResult> results, string? sortBy)
        => sortBy switch
        {
            "price_asc"  => results.OrderBy(r => r.Price),
            "price_desc" => results.OrderByDescending(r => r.Price),
            "name"       => results.OrderBy(r => r.Name),
            _            => results
        };

    private static void UpdateLocalCache(IEnumerable<ProductSearchResult> results)
    {
        var expiresAt = DateTime.UtcNow.Add(LocalCacheTtl);
        foreach (var r in results.Where(r => !string.IsNullOrWhiteSpace(r.ProductUrl) && r.Price > 0))
            LocalCache[ScraperHelpers.NormalizeProductUrl(r.ProductUrl)] = new CachedResult(r, expiresAt);
    }

    private static string BuildQueryCacheKey(string query, ProductFilterRequest filter)
    {
        var raw  = $"q={query.ToLowerInvariant()}"
                 + $"|store={filter.StoreId?.ToString() ?? string.Empty}"
                 + $"|brand={filter.Brand ?? string.Empty}"
                 + $"|min={filter.MinPrice?.ToString() ?? string.Empty}"
                 + $"|max={filter.MaxPrice?.ToString() ?? string.Empty}"
                 + $"|sort={filter.SortBy ?? string.Empty}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"search:query:{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }

    private static ProductSearchResult CloneWithUrl(ProductSearchResult r, string url)
        => new()
        {
            Name        = r.Name,
            Description = r.Description,
            ImageUrl    = r.ImageUrl,
            Price       = r.Price,
            Currency    = r.Currency,
            StoreName   = r.StoreName,
            ProductUrl  = url,
            VariantInfo = r.VariantInfo,
            Rating      = r.Rating,
            ReviewCount = r.ReviewCount,
            InStock     = r.InStock
        };

    private sealed record CachedResult(ProductSearchResult Result, DateTime ExpiresAt);
}