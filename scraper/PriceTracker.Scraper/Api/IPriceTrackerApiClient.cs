namespace PriceTracker.Scraper.Api;

public interface IPriceTrackerApiClient
{
    Task<IReadOnlyList<ScrapeListingDto>> GetActiveListingsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ScrapeListingDto>> GetActiveListingsAsync(int? categoryId = null, Guid? storeId = null, decimal? minPrice = null, decimal? maxPrice = null, string? currencyCode = null, CancellationToken ct = default);
    Task PostPriceRecordAsync(CreatePriceRecordDto record, CancellationToken ct = default);
    Task PostScrapeLogAsync(CreateScrapeLogDto log, CancellationToken ct = default);
}
