namespace PriceTracker.Application.Interfaces.Services;

public interface IScraperService
{
    Task ScrapeAllActiveListingsAsync();
    Task ScrapeListingAsync(Guid listingId);
}