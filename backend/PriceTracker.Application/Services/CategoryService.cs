namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Categories;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
        => _categoryRepository = categoryRepository;

    public async Task<IEnumerable<CategoryResponse>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(MapToResponse);
    }

    public async Task<CategoryResponse> GetByIdAsync(long categoryId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId)
            ?? throw new NotFoundException(nameof(Category), categoryId);

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        if (await _categoryRepository.ExistsByNameAsync(request.Name))
            throw new ConflictException($"Category '{request.Name}' already exists.");

        var category = new Category { Name = request.Name };
        await _categoryRepository.AddAsync(category);
        return MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(long categoryId, UpdateCategoryRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId)
            ?? throw new NotFoundException(nameof(Category), categoryId);

        if (await _categoryRepository.ExistsByNameAsync(request.Name))
            throw new ConflictException($"Category '{request.Name}' already exists.");

        category.Name = request.Name;
        await _categoryRepository.UpdateAsync(category);
        return MapToResponse(category);
    }

    public async Task DeleteAsync(long categoryId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId)
            ?? throw new NotFoundException(nameof(Category), categoryId);

        await _categoryRepository.DeleteAsync(category);
    }

    private static CategoryResponse MapToResponse(Category category) => new()
    {
        CategoryId = category.CategoryId,
        Name       = category.Name
    };
}