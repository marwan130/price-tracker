namespace PriceTracker.Application.Services;

using PriceTracker.Application.Interfaces.Services;

public class ScraperService : IScraperService
{
    public Task ScrapeAllActiveListingsAsync()
        => Task.CompletedTask;

    public Task ScrapeListingAsync(Guid listingId)
        => Task.CompletedTask;
}