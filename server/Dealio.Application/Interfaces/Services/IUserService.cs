namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Users;

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(Guid userId);
    Task<UserResponse> UpdateAsync(Guid userId, UpdateUserRequest request);
    Task DeactivateAsync(Guid userId);
    Task<bool> SetTrackingPausedAsync(Guid userId, bool paused);
    Task<DateTime> RequestDeletionAsync(Guid userId);
    Task CancelDeletionAsync(Guid userId);
}
