namespace PriceTracker.Application.Interfaces.Repositories;

using PriceTracker.Application.DTOs.PriceHistory;
using PriceTracker.Domain.Entities;

public interface IPriceHistoryRepository
{
    Task<IEnumerable<PriceHistory>> GetByListingIdAsync(Guid listingId, PriceHistoryFilterRequest filter);
    Task<PriceHistory?>             GetByIdAsync(long id);
    Task<PriceHistory?>             GetLatestByListingIdAsync(Guid listingId);
    Task<decimal?>                  GetLowestPriceByListingIdAsync(Guid listingId);
    Task<decimal?>                  GetHighestPriceByListingIdAsync(Guid listingId);
    Task<decimal?>                  GetAveragePriceByListingIdAsync(Guid listingId);
    Task<bool>                      ExistsAsync(Guid listingId, DateTime recordedAt);
    Task                            AddAsync(PriceHistory priceHistory);
}