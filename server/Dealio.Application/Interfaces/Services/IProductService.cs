namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;

public interface IProductService
{
    Task<PagedResult<ProductSummaryResponse>> GetAllAsync(ProductFilterRequest filter, PaginationRequest pagination);
    Task<ProductResponse>                     GetByIdAsync(Guid productId);
    Task<ProductResponse>                     GetByUrlAsync(string url);
    Task<ProductResponse>                     CreateAsync(CreateProductRequest request);
    Task<ProductResponse>                     UpdateAsync(Guid productId, UpdateProductRequest request);
    Task                                      DeleteAsync(Guid productId);
}