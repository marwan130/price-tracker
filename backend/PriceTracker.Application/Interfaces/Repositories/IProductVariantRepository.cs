namespace PriceTracker.Application.Interfaces.Repositories;

using PriceTracker.Domain.Entities;

public interface IProductVariantRepository
{
    Task<IEnumerable<ProductVariant>> GetByProductIdAsync(Guid productId);
    Task<ProductVariant?>             GetByIdAsync(Guid variantId);
    Task<ProductVariant?>             GetByIdWithAttributesAsync(Guid variantId);
    Task<bool>                        ExistsBySkuAsync(Guid productId, string sku);
    Task                              AddAsync(ProductVariant variant);
    Task                              UpdateAsync(ProductVariant variant);
    Task                              DeleteAsync(ProductVariant variant);
}