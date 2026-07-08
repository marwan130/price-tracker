namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Internal;
using PriceTracker.Application.DTOs.Listings;

public interface IListingService
{
    Task<IEnumerable<ScrapeListingResponse>> GetActiveForScrapingAsync(int page = 0, int size = 100);
    Task<IEnumerable<ScrapeListingResponse>> GetActiveForScrapingFilteredAsync(int? categoryId = null, Guid? storeId = null, decimal? minPrice = null, decimal? maxPrice = null, string? currencyCode = null, int page = 0, int size = 100);
    Task<IEnumerable<ListingResponse>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<ListingResponse>> GetByProductUrlAsync(string url);
    Task<IEnumerable<ListingResponse>> GetByVariantIdAsync(Guid variantId);
    Task<IEnumerable<ListingResponse>> GetByStoreIdAsync(Guid storeId);
    Task<ListingResponse>              GetByIdAsync(Guid listingId);
    Task<ListingResponse>              CreateAsync(CreateListingRequest request);
    Task<ListingResponse>              UpdateAsync(Guid listingId, UpdateListingRequest request);
    Task                               DeleteAsync(Guid listingId);
}