namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Stores;

public interface IStoreService
{
    Task<IEnumerable<StoreResponse>> GetAllAsync();
    Task<StoreResponse>              GetByIdAsync(Guid storeId);
    Task<StoreResponse>              CreateAsync(CreateStoreRequest request);
    Task<StoreResponse>              UpdateAsync(Guid storeId, UpdateStoreRequest request);
    Task                             DeleteAsync(Guid storeId);
}