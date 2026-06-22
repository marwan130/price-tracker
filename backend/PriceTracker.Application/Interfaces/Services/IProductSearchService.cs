namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Products;

public interface IProductSearchService
{
    Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(string query, CancellationToken ct = default);
    Task<ProductSearchResult?> SearchByUrlAsync(string url, CancellationToken ct = default);
}
