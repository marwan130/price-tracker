namespace Dealio.Application.Interfaces.Repositories;

using Dealio.Application.DTOs.PriceHistory;
using Dealio.Application.DTOs.Common;
using Dealio.Domain.Entities;

public interface IPriceHistoryRepository
{
    Task<IEnumerable<PriceHistory>> GetByListingIdAsync(Guid listingId, PriceHistoryFilterRequest filter);
    Task<(IReadOnlyList<PriceHistory> Content, long TotalElements)> GetByListingIdPageAsync(Guid listingId, PriceHistoryFilterRequest filter, PaginationRequest pagination);
    Task<PriceHistory?>             GetByIdAsync(long id);
    Task<PriceHistory?>             GetLatestByListingIdAsync(Guid listingId);
    Task<PriceHistory?>             GetPreviousByListingIdAsync(Guid listingId, long latestRecordId);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<PriceHistory>>> GetLatestTwoByListingIdsAsync(IReadOnlyCollection<Guid> listingIds);
    Task<decimal?>                  GetLowestPriceByListingIdAsync(Guid listingId);
    Task<decimal?>                  GetHighestPriceByListingIdAsync(Guid listingId);
    Task<decimal?>                  GetAveragePriceByListingIdAsync(Guid listingId);
    Task<IReadOnlyList<RecentPriceDropResponse>> GetRecentDropsAsync(int size);
    Task<bool>                      ExistsAsync(Guid listingId, DateTime recordedAt);
    Task                            AddAsync(PriceHistory priceHistory);
}
