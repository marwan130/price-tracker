namespace Dealio.Application.Interfaces.Repositories;

using Dealio.Domain.Entities;

public interface IUserRepository
{
    Task<User?>              GetByIdAsync(Guid userId);
    Task<User?>              GetByEmailAsync(string email);
    Task<User?>              GetByGoogleSubjectAsync(string subject);
    Task<User?>              GetByEmailVerificationTokenHashAsync(string tokenHash);
    Task<User?>              GetByPasswordResetTokenHashAsync(string tokenHash);
    Task<bool>               ExistsByEmailAsync(string email);
    Task                     AddAsync(User user);
    Task                     UpdateAsync(User user);
    Task                     DeleteAsync(User user);
}
