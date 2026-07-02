namespace PriceTracker.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Domain.Entities;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<User?> GetByIdAsync(Guid userId)
        => await _context.Users.FindAsync(userId);

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users
                         .FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByEmailVerificationTokenHashAsync(string tokenHash)
        => await _context.Users
                         .FirstOrDefaultAsync(u =>
                             u.EmailVerificationTokenHash == tokenHash &&
                             u.EmailVerificationTokenExpiresAt != null &&
                             u.EmailVerificationTokenExpiresAt > DateTime.UtcNow);

    public async Task<User?> GetByPasswordResetTokenHashAsync(string tokenHash)
        => await _context.Users
                         .FirstOrDefaultAsync(u =>
                             u.PasswordResetTokenHash == tokenHash &&
                             u.PasswordResetTokenExpiresAt != null &&
                             u.PasswordResetTokenExpiresAt > DateTime.UtcNow);

    public async Task<bool> ExistsByEmailAsync(string email)
        => await _context.Users
                         .AnyAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}