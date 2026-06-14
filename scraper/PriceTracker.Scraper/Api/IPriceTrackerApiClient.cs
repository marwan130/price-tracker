namespace PriceTracker.Scraper.Api;

public interface IPriceTrackerApiClient
{
    Task<IReadOnlyList<ScrapeListingDto>> GetActiveListingsAsync(CancellationToken ct = default);
    Task PostPriceRecordAsync(CreatePriceRecordDto record, CancellationToken ct = default);
    Task PostScrapeLogAsync(CreateScrapeLogDto log, CancellationToken ct = default);
}
