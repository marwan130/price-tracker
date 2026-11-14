namespace Dealio.Application.Interfaces.Repositories;

using Dealio.Domain.Entities;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?>             GetByIdAsync(long categoryId);
    Task<bool>                  ExistsByNameAsync(string name);
    Task                        AddAsync(Category category);
    Task                        UpdateAsync(Category category);
    Task                        DeleteAsync(Category category);
}