namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Products;

public interface IProductSearchService
{
    Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(ProductFilterRequest filter, CancellationToken ct = default);
    Task<IEnumerable<ProductSearchResult>> SearchProgressivelyAsync(ProductFilterRequest filter, Func<ProductSearchResult, CancellationToken, Task> onCandidate, CancellationToken ct = default, Func<string, Exception, CancellationToken, Task>? onSourceFailure = null);
    bool HasMoreSearchPages(string? market, int page);
}
