namespace PriceTracker.Infrastructure.Persistence.Repositories;

using System.Security.Cryptography;
using System.Text;
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
            var minPrice = filter.MinPrice.Value;
            query = query.Where(p => p.Listings.Any(l => l.IsActive) &&
                !p.Listings.Any(l => l.IsActive && l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault() < minPrice));
        }

        if (filter.MaxPrice.HasValue)
        {
            var maxPrice = filter.MaxPrice.Value;
            query = query.Where(p => p.Listings.Any(l => l.IsActive && 
                l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault() <= maxPrice));
        }

        var total = await query.CountAsync();

        // Random feed: a client-supplied seed produces one stable shuffled
        // order for the whole filtered set, so successive pages continue
        // the same browsing sequence instead of re-rolling per request.
        if (filter.SortBy == "random")
        {
            var ids = await query.Select(p => p.ProductId).ToListAsync();
            var orderedIds = SeedShuffled(filter.RandomSeed, ids)
                .Skip(pagination.Page * pagination.Size)
                .Take(pagination.Size)
                .ToList();

            var randomItems = orderedIds.Count == 0
                ? []
                : await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .Include(p => p.Listings)
                        .ThenInclude(l => l.PriceHistories)
                    .Where(p => orderedIds.Contains(p.ProductId))
                    .ToListAsync();

            var byId = randomItems.ToDictionary(p => p.ProductId);
            var pageItems = orderedIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
            return PagedResult<Product>.From(pageItems, pagination.Page, pagination.Size, total);
        }

        IOrderedQueryable<Product> sortedQuery;
        if (filter.SortBy == "price_asc")
        {
            sortedQuery = query.OrderBy(p => p.Listings.Where(l => l.IsActive).Min(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault()) ?? decimal.MaxValue);
        }
        else if (filter.SortBy == "price_desc")
        {
            sortedQuery = query.OrderByDescending(p => p.Listings.Where(l => l.IsActive).Min(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault()) ?? decimal.MinValue);
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
            sortedQuery = query.OrderByDescending(p => p.Listings
                .Where(l => l.IsActive)
                .SelectMany(l => l.PriceHistories)
                .Max(ph => (DateTime?)ph.RecordedAt) ?? p.CreatedAt);
        }

        var items = await sortedQuery
            .Include(p => p.Listings)
                .ThenInclude(l => l.PriceHistories)
            .Skip(pagination.Page * pagination.Size)
            .Take(pagination.Size)
            .ToListAsync();

        return PagedResult<Product>.From(items, pagination.Page, pagination.Size, total);
    }

    private static IOrderedEnumerable<Guid> SeedShuffled(string? seed, IReadOnlyCollection<Guid> ids)
        => ids.OrderBy(id => Rank(seed, id));

    private static ulong Rank(string? seed, Guid id)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(seed ?? string.Empty, "|", id.ToString("N"))));
        return BitConverter.ToUInt64(bytes, 0);
    }

    public async Task<Product?> GetByIdAsync(Guid productId)
        => await _context.Products.FindAsync(productId);

    public async Task<Product?> GetByIdWithDetailsAsync(Guid productId)
        => await _context.Products
                         .Include(p => p.Category)
                         .Include(p => p.Images)
                         .Include(p => p.Listings)
                             .ThenInclude(l => l.PriceHistories)
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
