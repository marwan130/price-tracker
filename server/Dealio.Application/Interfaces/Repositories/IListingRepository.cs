namespace Dealio.Application.Interfaces.Repositories;

using Dealio.Domain.Entities;

public interface IListingRepository
{
    Task<IEnumerable<StoreProductListing>> GetAllAsync();
    Task<IEnumerable<StoreProductListing>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<StoreProductListing>> GetByStoreIdAsync(Guid storeId);
    Task<IEnumerable<StoreProductListing>> GetActiveListingsAsync(int page = 0, int size = 100);
    Task<IEnumerable<StoreProductListing>> GetActiveListingsFilteredByPreferencesAsync(string? query = null, int? categoryId = null, Guid? storeId = null, decimal? minPrice = null, decimal? maxPrice = null, string? currencyCode = null, int page = 0, int size = 100);
    Task<StoreProductListing?>             GetByIdAsync(Guid listingId);
    Task<StoreProductListing?>             GetByUrlAsync(string url);
    Task                                   AddAsync(StoreProductListing listing);
    Task                                   UpdateAsync(StoreProductListing listing);
    Task                                   DeleteAsync(StoreProductListing listing);
    Task                                   SetExcludedAsync(Guid listingId, bool excluded);
}
