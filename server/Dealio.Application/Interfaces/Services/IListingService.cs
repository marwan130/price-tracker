namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Internal;
using Dealio.Application.DTOs.Listings;

public interface IListingService
{
    Task<IEnumerable<ScrapeListingResponse>> GetActiveForScrapingAsync(int page = 0, int size = 100);
    Task<IEnumerable<ScrapeListingResponse>> GetActiveForScrapingFilteredAsync(string? query = null, int? categoryId = null, Guid? storeId = null, decimal? minPrice = null, decimal? maxPrice = null, string? currencyCode = null, int page = 0, int size = 100);
    Task<IEnumerable<ListingResponse>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<ListingResponse>> GetByStoreIdAsync(Guid storeId);
    Task<ListingResponse>              GetByIdAsync(Guid listingId);
    Task<ListingResponse>              CreateAsync(CreateListingRequest request);
    Task<ListingResponse>              UpdateAsync(Guid listingId, UpdateListingRequest request);
    Task                               DeleteAsync(Guid listingId);
}
