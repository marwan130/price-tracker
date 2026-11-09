namespace PriceTracker.Application.Interfaces.Services;

public interface IListingScrapeQueue
{
    void Stage(Guid listingId, string httpsUrl);
}
