namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Stores;

public interface IStoreService
{
    Task<IEnumerable<StoreResponse>> GetAllAsync();
    Task<StoreResponse>              GetByIdAsync(Guid storeId);
    Task<StoreResponse>              CreateAsync(CreateStoreRequest request);
    Task<StoreResponse>              UpdateAsync(Guid storeId, UpdateStoreRequest request);
    Task                             DeleteAsync(Guid storeId);
}