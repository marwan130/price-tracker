namespace PriceTracker.Application.Services;

using Microsoft.Extensions.Logging;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;

public class ProductSearchService : IProductSearchService
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedSearchResult> RecentSearchResults = new();
    private static readonly System.TimeSpan SearchResultCacheLifetime = System.TimeSpan.FromMinutes(30);

    private readonly ILogger<ProductSearchService> _logger;
    private readonly IProductRepository _productRepository;

    public ProductSearchService(
        ILogger<ProductSearchService> logger,
        IProductRepository productRepository)
    {
        _logger = logger;
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(string query, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching for products: {Query}", query);

        if (string.IsNullOrWhiteSpace(query))
            return [];

        var results = new List<ProductSearchResult>();

        try
        {
            var localPaged = await _productRepository.GetAllAsync(
                new ProductFilterRequest { Query = query },
                new PaginationRequest { Page = 0, Size = 50 }
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

        CacheSearchResults(results);
        return results;
    }

    public async Task<ProductSearchResult?> SearchByUrlAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching for product by URL: {Url}", url);

        if (string.IsNullOrWhiteSpace(url))
            return null;

        var normalizedUrl = NormalizeProductUrl(url);
        if (RecentSearchResults.TryGetValue(normalizedUrl, out var cached))
        {
            if (cached.ExpiresAt > System.DateTime.UtcNow)
                return CloneSearchResult(cached.Result, url);

            RecentSearchResults.TryRemove(normalizedUrl, out _);
        }

        return null;
    }

    private static void CacheSearchResults(IEnumerable<ProductSearchResult> results)
    {
        var expiresAt = System.DateTime.UtcNow.Add(SearchResultCacheLifetime);
        foreach (var result in results.Where(r => !string.IsNullOrWhiteSpace(r.ProductUrl) && r.Price > 0))
        {
            RecentSearchResults[NormalizeProductUrl(result.ProductUrl)] = new CachedSearchResult(result, expiresAt);
        }
    }

    private static string NormalizeProductUrl(string url)
    {
        if (!System.Uri.TryCreate(url, System.UriKind.Absolute, out var uri))
            return url.Trim();

        return uri.GetLeftPart(System.UriPartial.Path).TrimEnd('/');
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

    private sealed record CachedSearchResult(ProductSearchResult Result, System.DateTime ExpiresAt);
}
