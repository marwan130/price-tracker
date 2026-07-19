namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Categories;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class CategoryService : ICategoryService
{
    private const string AllCategoriesKey = "categories:all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService?       _cache;

    public CategoryService(ICategoryRepository categoryRepository, ICacheService? cache = null)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllAsync()
    {
        if (_cache is not null)
        {
            var cached = await _cache.GetAsync<List<CategoryResponse>>(AllCategoriesKey);
            if (cached is not null)
                return cached;
        }

        var categories = await _categoryRepository.GetAllAsync();
        var result = categories.Select(MapToResponse).ToList();

        if (_cache is not null)
            await _cache.SetAsync(AllCategoriesKey, result, CacheTtl);

        return result;
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

        if (_cache is not null)
            await _cache.RemoveAsync(AllCategoriesKey);

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

        if (_cache is not null)
            await _cache.RemoveAsync(AllCategoriesKey);

        return MapToResponse(category);
    }

    public async Task DeleteAsync(long categoryId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId)
            ?? throw new NotFoundException(nameof(Category), categoryId);

        await _categoryRepository.DeleteAsync(category);

        if (_cache is not null)
            await _cache.RemoveAsync(AllCategoriesKey);
    }

    private static CategoryResponse MapToResponse(Category category) => new()
    {
        CategoryId = category.CategoryId,
        Name       = category.Name
    };
}