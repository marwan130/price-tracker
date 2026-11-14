namespace Dealio.Application.Interfaces.Repositories;

using Dealio.Domain.Entities;

public interface IStoreRepository
{
    Task<IEnumerable<Store>> GetAllAsync();
    Task<Store?>             GetByIdAsync(Guid storeId);
    Task<IEnumerable<Store>> GetByCountryAsync(string country);
    Task                     AddAsync(Store store);
    Task                     UpdateAsync(Store store);
    Task                     DeleteAsync(Store store);
}