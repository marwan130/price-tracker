namespace PriceTracker.Infrastructure.Search;

using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Application.Scrapers;
using PriceTracker.Domain.Markets;

public sealed class LocalCatalogDiscoveryProvider(IListingRepository listings) : IVerifiedProductDiscoveryProvider
{
    public string Name => "LocalCatalog";

    public async Task<IReadOnlyList<ProductSearchResult>> SearchAsync(string query, string? market,
        CancellationToken cancellationToken, decimal? minPrice = null, decimal? maxPrice = null, int page = 0, string? categoryName = null)
    {
        if (page > 0 || string.IsNullOrWhiteSpace(query)) return [];
        cancellationToken.ThrowIfCancellationRequested();
        var result = await listings.GetActiveListingsFilteredByPreferencesAsync(
            query: query.Trim(), minPrice: minPrice, maxPrice: maxPrice, page: 0, size: 500);
        return result.Where(listing => !listing.IsExcluded && listing.Store.IsActive
                && (listing.Store.AutoSuspendedUntilUtc is null || listing.Store.AutoSuspendedUntilUtc <= DateTime.UtcNow)
                && Uri.TryCreate(listing.ProductUrl, UriKind.Absolute, out var uri)
                && ScraperHelpers.IsProductDetailUrl(uri.AbsoluteUri))
            .Select(listing =>
            {
                var latest = listing.PriceHistories.OrderByDescending(history => history.RecordedAt).FirstOrDefault();
                var marketInfo = Uri.TryCreate(listing.ProductUrl, UriKind.Absolute, out var productUri)
                    ? MiddleEastMarkets.FindByProductUrl(productUri)
                    : null;
                marketInfo ??= MiddleEastMarkets.FindByCountry(listing.Store.Country);
                return new ProductSearchResult
                {
                    ProductId = listing.ProductId,
                    Name = listing.Product.Name, Description = listing.Product.Description ?? string.Empty,
                    ImageUrl = listing.Product.Images.OrderByDescending(image => image.IsPrimary).FirstOrDefault()?.Url ?? string.Empty,
                    Price = latest?.Price ?? 0, Currency = latest?.CurrencyCode ?? listing.Store.CurrencyCode ?? string.Empty,
                    StoreName = listing.Store.Name, ProductUrl = listing.ProductUrl, MarketCode = marketInfo?.Code ?? string.Empty,
                    Country = marketInfo?.Country ?? listing.Store.Country, DiscoveryProvider = Name, IsVerified = latest is not null
                };
            })
            .Where(candidate => candidate.IsVerified && (string.IsNullOrWhiteSpace(market)
                || string.Equals(candidate.MarketCode, market, StringComparison.OrdinalIgnoreCase)))
            .Where(candidate => string.IsNullOrWhiteSpace(categoryName)
                || string.Equals(ScraperHelpers.InferCategoryName(candidate.Name, candidate.ProductUrl), categoryName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public bool HasMore(string? market, int page) => false;
}
