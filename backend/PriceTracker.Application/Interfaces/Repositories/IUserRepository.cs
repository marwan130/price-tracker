namespace PriceTracker.Application.Interfaces.Repositories;

using PriceTracker.Domain.Entities;

public interface IUserRepository
{
    Task<User?>              GetByIdAsync(Guid userId);
    Task<User?>              GetByEmailAsync(string email);
    Task<bool>               ExistsByEmailAsync(string email);
    Task                     AddAsync(User user);
    Task                     UpdateAsync(User user);
    Task                     DeleteAsync(User user);
}