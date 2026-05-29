namespace PriceTracker.Application.Interfaces.Repositories;

using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Domain.Entities;

public interface IProductRepository
{
    Task<PagedResult<Product>> GetAllAsync(ProductFilterRequest filter, PaginationRequest pagination);
    Task<Product?>             GetByIdAsync(Guid productId);
    Task<Product?>             GetByIdWithDetailsAsync(Guid productId);
    Task                       AddAsync(Product product);
    Task                       UpdateAsync(Product product);
    Task                       DeleteAsync(Product product);
}