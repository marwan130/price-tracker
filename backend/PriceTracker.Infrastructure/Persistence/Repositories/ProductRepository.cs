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

        if (filter.StoreId.HasValue)
            query = query.Where(p => p.Listings.Any(l => l.StoreId == filter.StoreId.Value && l.IsActive));

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Listings
                .Where(l => l.IsActive)
                .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault())
                .Min() >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Listings
                .Where(l => l.IsActive)
                .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault())
                .Min() <= filter.MaxPrice.Value);
        }

        var total = await query.CountAsync();

        IOrderedQueryable<Product> sortedQuery;
        if (filter.SortBy == "price_asc")
        {
            sortedQuery = query.OrderBy(p => p.Listings
                .Where(l => l.IsActive)
                .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault())
                .Min() ?? decimal.MaxValue);
        }
        else if (filter.SortBy == "price_desc")
        {
            sortedQuery = query.OrderByDescending(p => p.Listings
                .Where(l => l.IsActive)
                .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault())
                .Min() ?? decimal.MinValue);
        }
        else if (filter.SortBy == "name")
        {
            sortedQuery = query.OrderBy(p => p.Name);
        }
        else if (filter.SortBy == "stores")
        {
            sortedQuery = query.OrderByDescending(p => p.Listings.Count(l => l.IsActive));
        }
        else
        {
            sortedQuery = query.OrderByDescending(p => p.CreatedAt);
        }

        var items = await sortedQuery
            .Include(p => p.Listings)
                .ThenInclude(l => l.PriceHistories)
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
                         .Include(p => p.Listings)
                             .ThenInclude(l => l.PriceHistories)
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