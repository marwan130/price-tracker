namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly ApplicationDbContext _context;

    public ProductVariantRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<ProductVariant>> GetByProductIdAsync(Guid productId)
        => await _context.ProductVariants
                         .AsNoTracking()
                         .Include(v => v.VariantAttributes)
                             .ThenInclude(va => va.AttributeValue)
                                 .ThenInclude(av => av.AttributeType)
                         .Where(v => v.ProductId == productId)
                         .ToListAsync();

    public async Task<ProductVariant?> GetByIdAsync(Guid variantId)
        => await _context.ProductVariants.FindAsync(variantId);

    public async Task<ProductVariant?> GetByIdWithAttributesAsync(Guid variantId)
        => await _context.ProductVariants
                         .Include(v => v.VariantAttributes)
                             .ThenInclude(va => va.AttributeValue)
                                 .ThenInclude(av => av.AttributeType)
                         .FirstOrDefaultAsync(v => v.VariantId == variantId);

    public async Task<bool> ExistsBySkuAsync(Guid productId, string sku)
        => await _context.ProductVariants
                         .AnyAsync(v => v.ProductId == productId && v.Sku == sku);

    public async Task AddAsync(ProductVariant variant)
    {
        await _context.ProductVariants.AddAsync(variant);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ProductVariant variant)
    {
        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();
    }
}