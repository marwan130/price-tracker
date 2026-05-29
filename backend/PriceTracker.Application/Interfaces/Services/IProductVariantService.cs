namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Variants;

public interface IProductVariantService
{
    Task<IEnumerable<VariantResponse>> GetByProductIdAsync(Guid productId);
    Task<VariantResponse>              GetByIdAsync(Guid variantId);
    Task<VariantResponse>              CreateAsync(Guid productId, CreateVariantRequest request);
    Task<VariantResponse>              UpdateAsync(Guid variantId, UpdateVariantRequest request);
    Task                               DeleteAsync(Guid variantId);
}