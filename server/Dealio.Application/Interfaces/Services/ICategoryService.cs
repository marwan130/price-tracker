namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Categories;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponse>> GetAllAsync();
    Task<CategoryResponse>              GetByIdAsync(long categoryId);
    Task<CategoryResponse>              CreateAsync(CreateCategoryRequest request);
    Task<CategoryResponse>              UpdateAsync(long categoryId, UpdateCategoryRequest request);
    Task                                DeleteAsync(long categoryId);
}