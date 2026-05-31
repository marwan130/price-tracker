namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class StoreRepository : IStoreRepository
{
    private readonly ApplicationDbContext _context;

    public StoreRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<Store>> GetAllAsync()
        => await _context.Stores
                         .AsNoTracking()
                         .Include(s => s.Currency)
                         .OrderBy(s => s.Name)
                         .ToListAsync();

    public async Task<Store?> GetByIdAsync(Guid storeId)
        => await _context.Stores
                         .Include(s => s.Currency)
                         .FirstOrDefaultAsync(s => s.StoreId == storeId);

    public async Task<IEnumerable<Store>> GetByCountryAsync(string country)
        => await _context.Stores
                         .AsNoTracking()
                         .Where(s => s.Country == country)
                         .ToListAsync();

    public async Task AddAsync(Store store)
    {
        await _context.Stores.AddAsync(store);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Store store)
    {
        _context.Stores.Update(store);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Store store)
    {
        _context.Stores.Remove(store);
        await _context.SaveChangesAsync();
    }
}