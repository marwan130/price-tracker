namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class AttributeTypeRepository : IAttributeTypeRepository
{
    private readonly ApplicationDbContext _context;

    public AttributeTypeRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<AttributeType>> GetAllAsync()
        => await _context.AttributeTypes
                         .AsNoTracking()
                         .Include(a => a.Values)
                         .OrderBy(a => a.Name)
                         .ToListAsync();

    public async Task<AttributeType?> GetByIdAsync(long attributeTypeId)
        => await _context.AttributeTypes
                         .Include(a => a.Values)
                         .FirstOrDefaultAsync(a => a.AttributeTypeId == attributeTypeId);

    public async Task<bool> ExistsByNameAsync(string name)
        => await _context.AttributeTypes
                         .AnyAsync(a => a.Name == name);

    public async Task AddAsync(AttributeType attributeType)
    {
        await _context.AttributeTypes.AddAsync(attributeType);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AttributeType attributeType)
    {
        _context.AttributeTypes.Update(attributeType);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(AttributeType attributeType)
    {
        _context.AttributeTypes.Remove(attributeType);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AttributeValue>> GetValuesByTypeIdAsync(long attributeTypeId)
        => await _context.AttributeValues
                         .AsNoTracking()
                         .Include(av => av.AttributeType)
                         .Where(av => av.AttributeTypeId == attributeTypeId)
                         .OrderBy(av => av.Value)
                         .ToListAsync();

    public async Task<AttributeValue?> GetValueByIdAsync(long attributeValueId)
        => await _context.AttributeValues
                         .Include(av => av.AttributeType)
                         .FirstOrDefaultAsync(av => av.AttributeValueId == attributeValueId);

    public async Task<bool> ExistsValueAsync(long attributeTypeId, string value)
        => await _context.AttributeValues
                         .AnyAsync(av => av.AttributeTypeId == attributeTypeId && av.Value == value);

    public async Task AddValueAsync(AttributeValue attributeValue)
    {
        await _context.AttributeValues.AddAsync(attributeValue);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateValueAsync(AttributeValue attributeValue)
    {
        _context.AttributeValues.Update(attributeValue);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteValueAsync(AttributeValue attributeValue)
    {
        _context.AttributeValues.Remove(attributeValue);
        await _context.SaveChangesAsync();
    }
}