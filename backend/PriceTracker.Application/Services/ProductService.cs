namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class ProductService : IProductService
{
    private readonly IProductRepository  _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProductService(
        IProductRepository  productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository  = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<PagedResult<ProductSummaryResponse>> GetAllAsync(
        ProductFilterRequest filter,
        PaginationRequest    pagination)
    {
        var paged  = await _productRepository.GetAllAsync(filter, pagination);
        var mapped = paged.Content.Select(MapToSummary);
        return PagedResult<ProductSummaryResponse>.From(mapped, paged.Page, paged.Size, paged.TotalElements);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(productId)
            ?? throw new NotFoundException(nameof(Product), productId);

        return MapToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        if (request.CategoryId.HasValue)
            _ = await _categoryRepository.GetByIdAsync(request.CategoryId.Value)
                ?? throw new NotFoundException(nameof(Category), request.CategoryId.Value);

        var product = new Product
        {
            ProductId   = Guid.NewGuid(),
            Name        = request.Name,
            Brand       = request.Brand,
            CategoryId  = request.CategoryId,
            Description = request.Description,
            CreatedAt   = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);
        return MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid productId, UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new NotFoundException(nameof(Product), productId);

        if (request.CategoryId.HasValue)
            _ = await _categoryRepository.GetByIdAsync(request.CategoryId.Value)
                ?? throw new NotFoundException(nameof(Category), request.CategoryId.Value);

        product.Name        = request.Name;
        product.Brand       = request.Brand;
        product.CategoryId  = request.CategoryId;
        product.Description = request.Description;

        await _productRepository.UpdateAsync(product);
        return MapToResponse(product);
    }

    public async Task DeleteAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new NotFoundException(nameof(Product), productId);

        await _productRepository.DeleteAsync(product);
    }

    private static ProductSummaryResponse MapToSummary(Product product) => new()
    {
        ProductId    = product.ProductId,
        Name         = product.Name,
        Brand        = product.Brand,
        Category     = product.Category?.Name,
        PrimaryImage = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                    ?? product.Images.FirstOrDefault()?.Url
    };

    private static ProductResponse MapToResponse(Product product) => new()
    {
        ProductId    = product.ProductId,
        Name         = product.Name,
        Brand        = product.Brand,
        Category     = product.Category?.Name,
        Description  = product.Description,
        PrimaryImage = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                    ?? product.Images.FirstOrDefault()?.Url,
        CreatedAt    = product.CreatedAt
    };
}