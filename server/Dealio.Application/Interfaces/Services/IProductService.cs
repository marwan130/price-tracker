namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.Products;

public interface IProductService
{
    Task<PagedResult<ProductSummaryResponse>> GetAllAsync(ProductFilterRequest filter, PaginationRequest pagination);
    Task<ProductResponse>                     GetByIdAsync(Guid productId);
    Task<ProductResponse>                     ImportSearchResultAsync(ProductSearchResult searchResult);
    Task<ProductResponse>                     CreateAsync(CreateProductRequest request);
    Task<ProductResponse>                     UpdateAsync(Guid productId, UpdateProductRequest request);
    Task                                      DeleteAsync(Guid productId);
}
