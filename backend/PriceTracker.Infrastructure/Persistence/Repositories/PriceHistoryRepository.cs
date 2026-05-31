namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.DTOs.PriceHistory;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public PriceHistoryRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<PriceHistory>> GetByListingIdAsync(Guid listingId, PriceHistoryFilterRequest filter)
    {
        var query = _context.PriceHistories
                            .AsNoTracking()
                            .Where(p => p.ListingId == listingId)
                            .AsQueryable();

        if (filter.From.HasValue)
            query = query.Where(p => p.RecordedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(p => p.RecordedAt <= filter.To.Value);

        return await query
            .OrderBy(p => p.RecordedAt)
            .ToListAsync();
    }

    public async Task<PriceHistory?> GetByIdAsync(long id)
        => await _context.PriceHistories.FindAsync(id);

    public async Task<PriceHistory?> GetLatestByListingIdAsync(Guid listingId)
        => await _context.PriceHistories
                         .Where(p => p.ListingId == listingId)
                         .OrderByDescending(p => p.RecordedAt)
                         .FirstOrDefaultAsync();

    public async Task<decimal?> GetLowestPriceByListingIdAsync(Guid listingId)
        => await _context.PriceHistories
                         .Where(p => p.ListingId == listingId)
                         .MinAsync(p => (decimal?)p.Price);

    public async Task<decimal?> GetHighestPriceByListingIdAsync(Guid listingId)
        => await _context.PriceHistories
                         .Where(p => p.ListingId == listingId)
                         .MaxAsync(p => (decimal?)p.Price);

    public async Task<decimal?> GetAveragePriceByListingIdAsync(Guid listingId)
        => await _context.PriceHistories
                         .Where(p => p.ListingId == listingId)
                         .AverageAsync(p => (decimal?)p.Price);

    public async Task<bool> ExistsAsync(Guid listingId, DateTime recordedAt)
        => await _context.PriceHistories
                         .AnyAsync(p => p.ListingId == listingId && p.RecordedAt == recordedAt);

    public async Task AddAsync(PriceHistory priceHistory)
    {
        await _context.PriceHistories.AddAsync(priceHistory);
        await _context.SaveChangesAsync();
    }
}