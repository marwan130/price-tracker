namespace Dealio.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Domain.Entities;
using Dealio.Domain.Enums;

public class ScrapeLogRepository : IScrapeLogRepository
{
    private readonly ApplicationDbContext _context;

    public ScrapeLogRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<ScrapeLog>> GetAllAsync()
        => await _context.ScrapeLogs
                         .AsNoTracking()
                         .Include(s => s.Store)
                         .OrderByDescending(s => s.StartedAt)
                         .ToListAsync();

    public async Task<IEnumerable<ScrapeLog>> GetByStoreIdAsync(Guid storeId)
        => await _context.ScrapeLogs
                         .AsNoTracking()
                         .Include(s => s.Store)
                         .Where(s => s.StoreId == storeId)
                         .OrderByDescending(s => s.StartedAt)
                         .ToListAsync();

    public async Task<IEnumerable<ScrapeLog>> GetByStatusAsync(ScrapeStatus status)
        => await _context.ScrapeLogs
                         .AsNoTracking()
                         .Include(s => s.Store)
                         .Where(s => s.Status == status)
                         .OrderByDescending(s => s.StartedAt)
                         .ToListAsync();

    public async Task<ScrapeLog?> GetByIdAsync(long logId)
        => await _context.ScrapeLogs
                         .Include(s => s.Store)
                         .FirstOrDefaultAsync(s => s.LogId == logId);

    public async Task AddAsync(ScrapeLog scrapeLog)
    {
        await _context.ScrapeLogs.AddAsync(scrapeLog);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ScrapeLog scrapeLog)
    {
        _context.ScrapeLogs.Update(scrapeLog);
        await _context.SaveChangesAsync();
    }
}