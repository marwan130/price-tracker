namespace PriceTracker.Application.Services;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Currencies;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Application.Scrapers;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Markets;

public class ProductSearchService : IProductSearchService
{
    private static readonly TimeSpan RedisCacheTtl  = TimeSpan.FromMinutes(5);

    private readonly ILogger<ProductSearchService>  _logger;
    private readonly ICategoryRepository            _categoryRepository;
    private readonly IStoreRepository               _storeRepository;
    private readonly IListingRepository             _listingRepository;
    private readonly ICacheService?                 _cache;
    private readonly ICurrencyConversionService?    _currencyConversion;
    private readonly IEnumerable<IProductDiscoveryProvider> _discoveryProviders;

    public ProductSearchService(
        ILogger<ProductSearchService>  logger,
        ICategoryRepository            categoryRepository,
        IStoreRepository               storeRepository,
        IListingRepository             listingRepository,
        ICacheService?                 cache = null,
        ICurrencyConversionService?    currencyConversion = null,
        IEnumerable<IProductDiscoveryProvider>? discoveryProviders = null)
    {
        _logger                 = logger;
        _categoryRepository     = categoryRepository;
        _storeRepository        = storeRepository;
        _listingRepository      = listingRepository;
        _cache                  = cache;
        _currencyConversion     = currencyConversion;
        _discoveryProviders = discoveryProviders ?? [];
    }

    public async Task<IEnumerable<ProductSearchResult>> SearchProgressivelyAsync(
        ProductFilterRequest filter,
        Func<ProductSearchResult, CancellationToken, Task> onCandidate,
        CancellationToken ct = default,
        Func<string, Exception, CancellationToken, Task>? onSourceFailure = null)
    {
        var query = filter.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return [];

        var category = filter.CategoryId.HasValue
            ? await _categoryRepository.GetByIdAsync(filter.CategoryId.Value)
            : null;
        if (filter.CategoryId.HasValue && category is null) return [];

        var found = new ConcurrentDictionary<string, ProductSearchResult>(StringComparer.OrdinalIgnoreCase);
        IProductDiscoveryProvider[] providers = _discoveryProviders.ToArray();
        var providerTasks = providers.Select(async provider =>
        {
            try
            {
                var candidates = await FilterByPriceRangeAsync(
                    await provider.SearchAsync(query, filter.Market, ct, filter.MinPrice, filter.MaxPrice, filter.Page, category?.Name),
                    filter, ct);
                foreach (var candidate in candidates)
                {
                    ApplyUrlMarket(candidate);
                    if (!SearchCandidateTitleMatcher.IsRelevantToSearch(candidate.Name, query)) continue;
                    if (!MatchesMarket(candidate.MarketCode, filter.Market)) continue;
                    if (category is not null && !string.Equals(ScraperHelpers.InferCategoryName(candidate.Name, candidate.ProductUrl), category.Name, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!ScraperHelpers.IsProductDetailUrl(candidate.ProductUrl)) continue;
                    candidate.IsVerified = false;
                    var key = ScraperHelpers.NormalizeProductUrl(candidate.ProductUrl);
                    if (!found.TryAdd(key, candidate)) continue;
                    await onCandidate(candidate, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Discovery provider {Provider} failed for a search", provider.Name);
                if (onSourceFailure is not null) await onSourceFailure(provider.Name, ex, ct);
            }
        }).ToList();

        await Task.WhenAll(providerTasks);
        return SortResults(found.Values, filter.SortBy).ToArray();
    }

    public bool HasMoreSearchPages(string? market, int page)
        => _discoveryProviders.Any(provider => provider.HasMore(market, page));

    public async Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(
        ProductFilterRequest filter, CancellationToken ct = default)
    {
        var query = filter.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return [];

        var category = filter.CategoryId.HasValue
            ? await _categoryRepository.GetByIdAsync(filter.CategoryId.Value)
            : null;
        if (filter.CategoryId.HasValue && category is null) return [];


        var cacheKey = BuildQueryCacheKey(query, filter);
        if (_cache is not null)
        {
            var cached = await _cache.GetAsync<List<ProductSearchResult>>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Product search cache hit for query");
                return SortResults(cached, filter.SortBy);
            }
        }

        var discoveries = new ConcurrentDictionary<string, ProductSearchResult>(StringComparer.OrdinalIgnoreCase);
        await Task.WhenAll(_discoveryProviders.Select(async provider =>
        {
            try
            {
                foreach (var candidate in await FilterByPriceRangeAsync(
                    await provider.SearchAsync(query, filter.Market, ct, filter.MinPrice, filter.MaxPrice, filter.Page, category?.Name),
                    filter, ct))
                {
                    ApplyUrlMarket(candidate);
                    if (!SearchCandidateTitleMatcher.IsRelevantToSearch(candidate.Name, query)
                        || !MatchesMarket(candidate.MarketCode, filter.Market)
                        || !ScraperHelpers.IsProductDetailUrl(candidate.ProductUrl)) continue;
                    candidate.IsVerified = false;
                    discoveries.TryAdd(ScraperHelpers.NormalizeProductUrl(candidate.ProductUrl), candidate);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Discovery provider {Provider} failed for a search", provider.Name); }
        }));

        var liveResults = discoveries.Values.ToList();
        await MergeExistingListingsAsync(liveResults);
        var filtered = Deduplicate(await ApplyFiltersAsync(liveResults, filter, ct));

        if (_cache is not null && filtered.Count > 0)
            await _cache.SetAsync(cacheKey, filtered, RedisCacheTtl, ct);

        return SortResults(filtered, filter.SortBy);
    }

    private async Task<List<ProductSearchResult>> ApplyFiltersAsync(
        IEnumerable<ProductSearchResult> results, ProductFilterRequest filter, CancellationToken ct)
    {
        var q = results.Where(r => !string.IsNullOrWhiteSpace(r.Name));

        if (filter.StoreId.HasValue)
        {
            var store = await _storeRepository.GetByIdAsync(filter.StoreId.Value);
            q = store is null ? []
              : q.Where(r => string.Equals(r.StoreName, store.Name, StringComparison.OrdinalIgnoreCase)
                          || string.Equals(r.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Market))
            q = q.Where(r => string.Equals(r.MarketCode, filter.Market, StringComparison.OrdinalIgnoreCase));

        if (filter.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(filter.CategoryId.Value);
            q = category is null ? []
              : q.Where(r => string.Equals(r.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(ScraperHelpers.InferCategoryName(r.Name, r.ProductUrl),
                                 category.Name, StringComparison.OrdinalIgnoreCase));
        }

        q = await FilterByPriceRangeAsync(q, filter, ct);

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

            foreach (var result in results.Where(r => !string.Equals(r.DiscoveryProvider, "LocalCatalog", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(r.StoreName, "Local Catalog", StringComparison.OrdinalIgnoreCase)))
            {
                if (!listings.TryGetValue(ScraperHelpers.NormalizeProductUrl(result.ProductUrl), out var listing)) continue;
                var latest = listing.PriceHistories.OrderByDescending(history => history.RecordedAt).FirstOrDefault();
                result.ProductId = listing.ProductId;
                result.ProductUrl = listing.ProductId.ToString();
                if (listing.IsActive && !listing.IsExcluded && latest is not null)
                {
                    result.Name = listing.Product.Name;
                    result.StoreName = listing.Store.Name;
                    result.Country = listing.Store.Country;
                    result.MarketCode = NormalizeMarket(listing.Store.Country);
                    result.Price = latest.Price;
                    result.Currency = latest.CurrencyCode;
                    result.IsVerified = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to merge live results with existing listings");
        }
    }

    private static List<ProductSearchResult> Deduplicate(IEnumerable<ProductSearchResult> results)
        => results
            .GroupBy(r => ScraperHelpers.NormalizeProductUrl(r.ProductUrl), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

    private static IEnumerable<ProductSearchResult> SortResults(IEnumerable<ProductSearchResult> results, string? sortBy)
        => sortBy switch
        {
            "price_asc"  => results.OrderBy(r => r.Currency, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Price <= 0 ? decimal.MaxValue : r.Price),
            "price_desc" => results.OrderBy(r => r.Currency, StringComparer.OrdinalIgnoreCase).ThenByDescending(r => r.Price),
            "verified"   => results.OrderBy(r => r.Currency, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Price <= 0 ? decimal.MaxValue : r.Price),
            "store"      => results.OrderBy(r => r.StoreName).ThenBy(r => r.Name),
            "name"       => results.OrderBy(r => r.Name),
            _            => results.OrderBy(r => r.Currency, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Price <= 0 ? decimal.MaxValue : r.Price)
        };

    private static string BuildQueryCacheKey(string query, ProductFilterRequest filter)
    {
        var raw  = $"q={query.ToLowerInvariant()}"
                 + $"|store={filter.StoreId?.ToString() ?? string.Empty}"
                 + $"|brand={filter.Brand ?? string.Empty}"
                 + $"|min={filter.MinPrice?.ToString() ?? string.Empty}"
                 + $"|max={filter.MaxPrice?.ToString() ?? string.Empty}"
                 + $"|market={filter.Market ?? string.Empty}"
                 + $"|category={filter.CategoryId?.ToString() ?? string.Empty}"
                 + $"|page={filter.Page}"
                 + $"|sort={filter.SortBy ?? string.Empty}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"search:query:v3:{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }

    private static string NormalizeMarket(string? country)
        => MiddleEastMarkets.FindByCountry(country)?.Code ?? country?.Trim().ToUpperInvariant() ?? string.Empty;

    private static void ApplyUrlMarket(ProductSearchResult candidate)
    {
        candidate.ProductUrl = System.Net.WebUtility.HtmlDecode(candidate.ProductUrl);
        var market = Uri.TryCreate(candidate.ProductUrl, UriKind.Absolute, out var uri)
            ? MiddleEastMarkets.FindByProductUrl(uri)
            : null;
        market ??= MiddleEastMarkets.All.FirstOrDefault(item =>
            string.Equals(item.CurrencyCode, candidate.Currency, StringComparison.OrdinalIgnoreCase));
        if (market is null) return;
        candidate.MarketCode = market.Code;
        candidate.Country = market.Country;
    }

    private static bool MatchesMarket(string? resultMarket, string? selectedMarket)
    {
        if (string.IsNullOrWhiteSpace(resultMarket)) return string.IsNullOrWhiteSpace(selectedMarket);
        var resultCode = NormalizeMarket(resultMarket);
        if (!MiddleEastMarkets.All.Any(market => string.Equals(market.Code, resultCode, StringComparison.OrdinalIgnoreCase))) return false;
        return string.IsNullOrWhiteSpace(selectedMarket)
            || string.Equals(resultCode, NormalizeMarket(selectedMarket), StringComparison.OrdinalIgnoreCase);
    }

    // Offer prices arrive in each store's currency, so a raw numeric comparison
    // mixes scales (AED 2,500 drops against an EGP 10,000 minimum). Convert to
    // the selected market's currency (EGP when no market is set, matching the
    // app's default), and keep candidates whose rate lookup fails instead of
    // silently hiding them.
    private async Task<IEnumerable<ProductSearchResult>> FilterByPriceRangeAsync(
        IEnumerable<ProductSearchResult> candidates, ProductFilterRequest filter, CancellationToken ct)
    {
        if (!filter.MinPrice.HasValue && !filter.MaxPrice.HasValue) return candidates;

        var priced = candidates.Where(c => c.Price > 0).ToList();
        if (priced.Count == 0 || _currencyConversion is null) return priced;

        var referenceCurrency = MiddleEastMarkets.Find(filter.Market)?.CurrencyCode ?? "EGP";
        var request = new CurrencyConversionRequest
        {
            TargetCurrency = referenceCurrency,
            Items = priced.Select(c => new CurrencyConversionInput { Amount = c.Price, CurrencyCode = c.Currency }).ToList()
        };

        List<CurrencyConversionResult>? items;
        try
        {
            items = (await _currencyConversion.ConvertAsync(request, ct)).Items?.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Price-range filter skipped currency conversion because exchange rates are unavailable");
            return priced;
        }

        var passing = new List<ProductSearchResult>();
        for (var i = 0; i < priced.Count; i++)
        {
            // One converted value per input, in order; a missing one means the
            // rate lookup failed, so the candidate stays rather than vanishing.
            var converted = items is { Count: var count } && count == priced.Count
                ? items[i].ConvertedAmount
                : null;
            if (converted is not { } amount
                || ScraperHelpers.IsWithinPriceRange(amount, filter.MinPrice, filter.MaxPrice)) passing.Add(priced[i]);
        }
        return passing;
    }
}
