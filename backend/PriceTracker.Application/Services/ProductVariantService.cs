namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Attributes;
using PriceTracker.Application.DTOs.Variants;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class ProductVariantService : IProductVariantService
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IProductRepository        _productRepository;

    public ProductVariantService(
        IProductVariantRepository variantRepository,
        IProductRepository        productRepository)
    {
        _variantRepository = variantRepository;
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<VariantResponse>> GetByProductIdAsync(Guid productId)
    {
        var variants = await _variantRepository.GetByProductIdAsync(productId);
        return variants.Select(MapToResponse);
    }

    public async Task<VariantResponse> GetByIdAsync(Guid variantId)
    {
        var variant = await _variantRepository.GetByIdWithAttributesAsync(variantId)
            ?? throw new NotFoundException(nameof(ProductVariant), variantId);

        return MapToResponse(variant);
    }

    public async Task<VariantResponse> CreateAsync(Guid productId, CreateVariantRequest request)
    {
        _ = await _productRepository.GetByIdAsync(productId)
            ?? throw new NotFoundException(nameof(Product), productId);

        if (request.Sku is not null && await _variantRepository.ExistsBySkuAsync(productId, request.Sku))
            throw new ConflictException($"A variant with SKU '{request.Sku}' already exists for this product.");

        var variant = new ProductVariant
        {
            VariantId         = Guid.NewGuid(),
            ProductId         = productId,
            Sku               = request.Sku,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow,
            VariantAttributes = request.AttributeValueIds
                .Select(id => new VariantAttribute { AttributeValueId = id })
                .ToList()
        };

        await _variantRepository.AddAsync(variant);
        return MapToResponse(variant);
    }

    public async Task<VariantResponse> UpdateAsync(Guid variantId, UpdateVariantRequest request)
    {
        var variant = await _variantRepository.GetByIdAsync(variantId)
            ?? throw new NotFoundException(nameof(ProductVariant), variantId);

        variant.Sku      = request.Sku;
        variant.IsActive = request.IsActive;

        await _variantRepository.UpdateAsync(variant);
        return MapToResponse(variant);
    }

    public async Task DeleteAsync(Guid variantId)
    {
        var variant = await _variantRepository.GetByIdAsync(variantId)
            ?? throw new NotFoundException(nameof(ProductVariant), variantId);

        await _variantRepository.DeleteAsync(variant);
    }

    private static VariantResponse MapToResponse(ProductVariant variant) => new()
    {
        VariantId  = variant.VariantId,
        ProductId  = variant.ProductId,
        Sku        = variant.Sku,
        IsActive   = variant.IsActive,
        CreatedAt  = variant.CreatedAt,
        Attributes = variant.VariantAttributes.Select(va => new AttributeValueResponse
        {
            AttributeValueId = va.AttributeValue?.AttributeValueId ?? va.AttributeValueId,
            AttributeTypeId  = va.AttributeValue?.AttributeTypeId  ?? 0,
            Type             = va.AttributeValue?.AttributeType?.Name ?? string.Empty,
            Value            = va.AttributeValue?.Value               ?? string.Empty
        })
    };
}