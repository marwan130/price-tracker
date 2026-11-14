namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly ApplicationDbContext _context;

    public CurrencyRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<Currency>> GetAllAsync()
        => await _context.Currencies
                         .AsNoTracking()
                         .OrderBy(c => c.Code)
                         .ToListAsync();

    public async Task<Currency?> GetByCodeAsync(string code)
        => await _context.Currencies.FindAsync(code);

    public async Task<bool> ExistsByCodeAsync(string code)
        => await _context.Currencies
                         .AnyAsync(c => c.Code == code);

    public async Task AddAsync(Currency currency)
    {
        await _context.Currencies.AddAsync(currency);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Currency currency)
    {
        _context.Currencies.Remove(currency);
        await _context.SaveChangesAsync();
    }
}