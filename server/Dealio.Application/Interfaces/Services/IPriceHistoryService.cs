namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.PriceHistory;

public interface IPriceHistoryService
{
    Task<PagedResult<PriceRecordResponse>> GetByListingIdAsync(Guid listingId, PriceHistoryFilterRequest filter, PaginationRequest pagination);
    Task<PriceRecordResponse>              GetByIdAsync(long id);
    Task<PriceTrendResponse>               GetTrendAsync(Guid listingId, PriceHistoryFilterRequest filter);
    Task<IReadOnlyList<RecentPriceDropResponse>> GetRecentDropsAsync(int size);
    Task<PriceRecordResponse>              CreateAsync(CreatePriceRecordRequest request);
}