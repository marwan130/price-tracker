namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class TrackingRepository : ITrackingRepository
{
    private readonly ApplicationDbContext _context;

    public TrackingRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<UserProductTracking>> GetByUserIdAsync(Guid userId)
        => await _context.UserProductTrackings
                         .AsNoTracking()
                         .Include(t => t.Product)
                         .Include(t => t.Variant)
                         .Include(t => t.Listing)
                             .ThenInclude(l => l!.Store)
                         .Include(t => t.Currency)
                         .Where(t => t.UserId == userId)
                         .OrderByDescending(t => t.CreatedAt)
                         .ToListAsync();

    public async Task<IEnumerable<UserProductTracking>> GetActiveTrackingsAsync()
        => await _context.UserProductTrackings
                         .AsNoTracking()
                         .Include(t => t.Product)
                         .Include(t => t.Variant)
                         .Include(t => t.Listing)
                         .Where(t => t.IsActive)
                         .ToListAsync();

    public async Task<UserProductTracking?> GetByIdAsync(Guid trackingId)
        => await _context.UserProductTrackings
                         .Include(t => t.Product)
                         .Include(t => t.Variant)
                         .Include(t => t.Listing)
                             .ThenInclude(l => l!.Store)
                         .Include(t => t.Currency)
                         .FirstOrDefaultAsync(t => t.TrackingId == trackingId);

    public async Task<bool> ExistsAsync(Guid userId, Guid productId, Guid? variantId, Guid? listingId)
        => await _context.UserProductTrackings
                         .AnyAsync(t =>
                             t.UserId    == userId    &&
                             t.ProductId == productId &&
                             t.VariantId == variantId &&
                             t.ListingId == listingId);

    public async Task AddAsync(UserProductTracking tracking)
    {
        await _context.UserProductTrackings.AddAsync(tracking);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserProductTracking tracking)
    {
        _context.UserProductTrackings.Update(tracking);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(UserProductTracking tracking)
    {
        _context.UserProductTrackings.Remove(tracking);
        await _context.SaveChangesAsync();
    }
}