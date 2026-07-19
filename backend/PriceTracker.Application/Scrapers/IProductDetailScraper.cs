namespace PriceTracker.Application.Scrapers;

using PriceTracker.Application.DTOs.Products;

public interface IProductDetailScraper
{
    Task<ProductSearchResult?> ScrapeAsync(string url, CancellationToken ct = default);
}