namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Listings;

public interface IListingService
{
    Task<IEnumerable<ListingResponse>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<ListingResponse>> GetByVariantIdAsync(Guid variantId);
    Task<IEnumerable<ListingResponse>> GetByStoreIdAsync(Guid storeId);
    Task<ListingResponse>              GetByIdAsync(Guid listingId);
    Task<ListingResponse>              CreateAsync(CreateListingRequest request);
    Task<ListingResponse>              UpdateAsync(Guid listingId, UpdateListingRequest request);
    Task                               DeleteAsync(Guid listingId);
}