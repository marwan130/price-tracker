namespace Dealio.Application.Services;

using Dealio.Application.DTOs.Users;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Domain.Entities;
using Dealio.Domain.Exceptions;

public class UserService : IUserService
{
    private readonly TimeSpan _deletionGracePeriod;
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository, int deletionGraceDays = 30)
    {
        _userRepository = userRepository;
        if (deletionGraceDays is < 1 or > 365)
            throw new ArgumentOutOfRangeException(nameof(deletionGraceDays), "Deletion grace period must be between 1 and 365 days.");
        _deletionGracePeriod = TimeSpan.FromDays(deletionGraceDays);
    }

    public async Task<UserResponse> GetByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        return MapToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        user.Name  = request.Name;
        user.Phone = request.Phone;

        await _userRepository.UpdateAsync(user);
        return MapToResponse(user);
    }

    public async Task DeactivateAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        user.IsActive = false;
        await _userRepository.UpdateAsync(user);
    }

    public async Task<bool> SetTrackingPausedAsync(Guid userId, bool paused)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);
        user.IsTrackingPaused = paused;
        await _userRepository.UpdateAsync(user);
        return user.IsTrackingPaused;
    }

    public async Task<DateTime> RequestDeletionAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);
        if (!user.IsActive)
            throw new BusinessRuleException("Account deletion is already scheduled or the account is inactive.");

        var now = DateTime.UtcNow;
        user.TrackingPausedBeforeDeletion = user.IsTrackingPaused;
        user.IsTrackingPaused = true;
        user.IsActive = false;
        user.DeletionRequestedAtUtc = now;
        user.DeletionScheduledAtUtc = now.Add(_deletionGracePeriod);
        await _userRepository.UpdateAsync(user);
        return user.DeletionScheduledAtUtc.Value;
    }

    public async Task CancelDeletionAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);
        if (user.DeletionScheduledAtUtc is null || user.DeletionScheduledAtUtc <= DateTime.UtcNow)
            throw new BusinessRuleException("There is no cancellable account deletion request.");

        user.IsActive = true;
        user.IsTrackingPaused = user.TrackingPausedBeforeDeletion;
        user.TrackingPausedBeforeDeletion = false;
        user.DeletionRequestedAtUtc = null;
        user.DeletionScheduledAtUtc = null;
        await _userRepository.UpdateAsync(user);
    }

    private static UserResponse MapToResponse(User user) => new()
    {
        UserId    = user.UserId,
        Name      = user.Name,
        Email     = user.Email,
        Phone     = user.Phone,
        Role      = user.Role.ToString(),
        IsActive  = user.IsActive,
        IsTrackingPaused = user.IsTrackingPaused,
        DeletionScheduledAtUtc = user.DeletionScheduledAtUtc,
        EmailVerified = user.EmailVerified,
        CreatedAt = user.CreatedAt
    };
}
