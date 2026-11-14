namespace Dealio.Application.Interfaces.Repositories;

using Dealio.Domain.Entities;

public interface ICurrencyRepository
{
    Task<IEnumerable<Currency>> GetAllAsync();
    Task<Currency?>             GetByCodeAsync(string code);
    Task<bool>                  ExistsByCodeAsync(string code);
    Task                        AddAsync(Currency currency);
    Task                        DeleteAsync(Currency currency);
}