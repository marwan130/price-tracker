namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Products;

public interface IProductDiscoveryProvider
{
    string Name { get; }
    Task<IReadOnlyList<ProductSearchResult>> SearchAsync(string query, string? market, CancellationToken cancellationToken, decimal? minPrice = null, decimal? maxPrice = null, int page = 0, string? categoryName = null);
    bool HasMore(string? market, int page);
}

public interface IVerifiedProductDiscoveryProvider : IProductDiscoveryProvider { }
