namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<PagedResult<Product>> GetAllAsync(ProductFilterRequest filter, PaginationRequest pagination)
    {
        var query = _context.Products
                            .AsNoTracking()
                            .Include(p => p.Category)
                            .Include(p => p.Images)
                            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Query))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{filter.Query}%"));

        if (!string.IsNullOrWhiteSpace(filter.Brand))
            query = query.Where(p => p.Brand == filter.Brand);

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(pagination.Page * pagination.Size)
            .Take(pagination.Size)
            .ToListAsync();

        return PagedResult<Product>.From(items, pagination.Page, pagination.Size, total);
    }

    public async Task<Product?> GetByIdAsync(Guid productId)
        => await _context.Products.FindAsync(productId);

    public async Task<Product?> GetByIdWithDetailsAsync(Guid productId)
        => await _context.Products
                         .Include(p => p.Category)
                         .Include(p => p.Images)
                         .Include(p => p.Variants)
                             .ThenInclude(v => v.VariantAttributes)
                                 .ThenInclude(va => va.AttributeValue)
                                     .ThenInclude(av => av.AttributeType)
                         .FirstOrDefaultAsync(p => p.ProductId == productId);

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }
}