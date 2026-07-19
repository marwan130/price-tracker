namespace PriceTracker.Application.Scrapers;

using PriceTracker.Application.DTOs.Products;

public interface ISearchScraper
{
    IReadOnlyList<StoreDescriptor> Stores { get; }
    Task<List<ProductSearchResult>> ScrapeAsync(StoreDescriptor store, string query, CancellationToken ct = default);
}