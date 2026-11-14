namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.DTOs.PriceHistory;
using PriceTracker.Application.DTOs.Common;
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

    public async Task<(IReadOnlyList<PriceHistory> Content, long TotalElements)> GetByListingIdPageAsync(
        Guid listingId, PriceHistoryFilterRequest filter, PaginationRequest pagination)
    {
        var query = _context.PriceHistories.AsNoTracking().Where(p => p.ListingId == listingId);
        if (filter.From.HasValue) query = query.Where(p => p.RecordedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(p => p.RecordedAt <= filter.To.Value);

        var total = await query.LongCountAsync();
        var content = await query.OrderBy(p => p.RecordedAt).ThenBy(p => p.Id)
            .Skip(pagination.Page * pagination.Size).Take(pagination.Size).ToListAsync();
        return (content, total);
    }

    public async Task<PriceHistory?> GetByIdAsync(long id)
        => await _context.PriceHistories.FindAsync(id);

    public async Task<PriceHistory?> GetLatestByListingIdAsync(Guid listingId)
        => await _context.PriceHistories
                         .Where(p => p.ListingId == listingId)
                         .OrderByDescending(p => p.RecordedAt)
                         .ThenByDescending(p => p.Id)
                         .FirstOrDefaultAsync();

    public async Task<PriceHistory?> GetPreviousByListingIdAsync(Guid listingId, long latestRecordId)
        => await _context.PriceHistories
                         .Where(p => p.ListingId == listingId && p.Id != latestRecordId)
                         .OrderByDescending(p => p.RecordedAt)
                         .ThenByDescending(p => p.Id)
                         .FirstOrDefaultAsync();

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<PriceHistory>>> GetLatestTwoByListingIdsAsync(IReadOnlyCollection<Guid> listingIds)
    {
        var ids = listingIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, IReadOnlyList<PriceHistory>>();

        var recent = await _context.PriceHistories
            .FromSqlInterpolated($"""
                SELECT selected.*
                FROM unnest({ids}) AS requested("ListingId")
                CROSS JOIN LATERAL (
                    SELECT price.*
                    FROM "PriceHistories" AS price
                    WHERE price."ListingId" = requested."ListingId"
                    ORDER BY price."RecordedAt" DESC
                    LIMIT 2
                ) AS selected
                """)
            .AsNoTracking()
            .ToListAsync();

        return recent
            .GroupBy(price => price.ListingId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<PriceHistory>)group
                .OrderByDescending(price => price.RecordedAt)
                .ToList());
    }

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

    public async Task<IReadOnlyList<RecentPriceDropResponse>> GetRecentDropsAsync(int size)
    {
        var sampleSize = Math.Clamp(size * 20, 50, 500);
        var records = await _context.PriceHistories
            .AsNoTracking()
            .Include(p => p.Listing)
                .ThenInclude(l => l.Product)
            .Include(p => p.Listing)
                .ThenInclude(l => l.Store)
            .OrderByDescending(p => p.RecordedAt)
            .Take(sampleSize)
            .ToListAsync();

        return records
            .GroupBy(record => record.ListingId)
            .Select(group => group.OrderByDescending(record => record.RecordedAt).Take(2).ToList())
            .Where(pair => pair.Count == 2 && pair[0].Price < pair[1].Price)
            .Select(pair =>
            {
                var latest = pair[0];
                var previous = pair[1];
                return new RecentPriceDropResponse
                {
                    ListingId     = latest.ListingId,
                    ProductId     = latest.Listing.ProductId,
                    ProductName   = latest.Listing.Product.Name,
                    StoreName     = latest.Listing.Store.Name,
                    PreviousPrice = previous.Price,
                    CurrentPrice  = latest.Price,
                    DropPercent   = previous.Price <= 0 ? 0 : Math.Round((previous.Price - latest.Price) / previous.Price * 100, 2),
                    CurrencyCode  = latest.CurrencyCode,
                    RecordedAt    = latest.RecordedAt
                };
            })
            .OrderByDescending(drop => drop.RecordedAt)
            .Take(size)
            .ToList();
    }

    public async Task<bool> ExistsAsync(Guid listingId, DateTime recordedAt)
        => await _context.PriceHistories
                         .AnyAsync(p => p.ListingId == listingId && p.RecordedAt == recordedAt);

    public async Task AddAsync(PriceHistory priceHistory)
    {
        await _context.PriceHistories.AddAsync(priceHistory);
        await _context.SaveChangesAsync();
    }
}
