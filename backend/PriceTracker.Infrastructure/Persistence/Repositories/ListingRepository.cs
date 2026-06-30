namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class ListingRepository : IListingRepository
{
    private readonly ApplicationDbContext _context;

    public ListingRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<StoreProductListing>> GetAllAsync()
        => await _context.StoreProductListings
                         .AsNoTracking()
                         .Include(l => l.Product)
                         .Include(l => l.Variant)
                         .Include(l => l.Store)
                         .Include(l => l.PriceHistories)
                         .ToListAsync();

    public async Task<IEnumerable<StoreProductListing>> GetByProductIdAsync(Guid productId)
        => await _context.StoreProductListings
                         .AsNoTracking()
                         .Include(l => l.Product)
                         .Include(l => l.Variant)
                         .Include(l => l.Store)
                         .Include(l => l.PriceHistories)
                         .Where(l => l.ProductId == productId)
                         .ToListAsync();

    public async Task<IEnumerable<StoreProductListing>> GetByVariantIdAsync(Guid variantId)
        => await _context.StoreProductListings
                         .AsNoTracking()
                         .Include(l => l.Product)
                         .Include(l => l.Variant)
                         .Include(l => l.Store)
                         .Include(l => l.PriceHistories)
                         .Where(l => l.VariantId == variantId)
                         .ToListAsync();

    public async Task<IEnumerable<StoreProductListing>> GetByStoreIdAsync(Guid storeId)
        => await _context.StoreProductListings
                         .AsNoTracking()
                         .Include(l => l.Product)
                         .Include(l => l.Variant)
                         .Include(l => l.Store)
                         .Include(l => l.PriceHistories)
                         .Where(l => l.StoreId == storeId)
                         .ToListAsync();

    public async Task<IEnumerable<StoreProductListing>> GetActiveListingsAsync(int page = 0, int size = 100)
        => await _context.StoreProductListings
                         .AsNoTracking()
                         .Include(l => l.Product)
                         .Include(l => l.Variant)
                         .Include(l => l.Store)
                         .Include(l => l.PriceHistories)
                         .Where(l => l.IsActive)
                         .OrderBy(l => l.ListingId)
                         .Skip(Math.Max(page, 0) * Math.Clamp(size, 1, 500))
                         .Take(Math.Clamp(size, 1, 500))
                         .ToListAsync();

    public async Task<StoreProductListing?> GetByIdAsync(Guid listingId)
        => await _context.StoreProductListings
                         .Include(l => l.Product)
                         .Include(l => l.Variant)
                         .Include(l => l.Store)
                         .Include(l => l.PriceHistories)
                         .FirstOrDefaultAsync(l => l.ListingId == listingId);

    public async Task<StoreProductListing?> GetByVariantAndStoreAsync(Guid variantId, Guid storeId)
        => await _context.StoreProductListings
                         .FirstOrDefaultAsync(l => l.VariantId == variantId && l.StoreId == storeId);

    public async Task<bool> ExistsAsync(Guid variantId, Guid storeId)
        => await _context.StoreProductListings
                         .AnyAsync(l => l.VariantId == variantId && l.StoreId == storeId);

    public async Task<StoreProductListing?> GetByUrlAsync(string url)
    {
        var normalizedUrl = NormalizeProductUrl(url);
        var listings = await _context.StoreProductListings
            .Include(l => l.Product)
            .Include(l => l.Variant)
            .Include(l => l.Store)
            .Include(l => l.PriceHistories)
            .Where(l => l.ProductUrl == url || l.ProductUrl == normalizedUrl)
            .ToListAsync();

        return listings.FirstOrDefault()
            ?? (await _context.StoreProductListings
                .Include(l => l.Product)
                .Include(l => l.Variant)
                .Include(l => l.Store)
                .Include(l => l.PriceHistories)
                .ToListAsync())
            .FirstOrDefault(l => NormalizeProductUrl(l.ProductUrl) == normalizedUrl);
    }

    public async Task AddAsync(StoreProductListing listing)
    {
        await _context.StoreProductListings.AddAsync(listing);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(StoreProductListing listing)
    {
        _context.StoreProductListings.Update(listing);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(StoreProductListing listing)
    {
        _context.StoreProductListings.Remove(listing);
        await _context.SaveChangesAsync();
    }

    private static string NormalizeProductUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim();

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }
}