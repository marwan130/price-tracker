namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<Category>> GetAllAsync()
        => await _context.Categories
                         .AsNoTracking()
                         .OrderBy(c => c.Name)
                         .ToListAsync();

    public async Task<Category?> GetByIdAsync(long categoryId)
        => await _context.Categories.FindAsync(categoryId);

    public async Task<bool> ExistsByNameAsync(string name)
        => await _context.Categories
                         .AnyAsync(c => c.Name == name);

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Category category)
    {
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}