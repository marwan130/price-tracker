namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Users;

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(Guid userId);
    Task<UserResponse> UpdateAsync(Guid userId, UpdateUserRequest request);
    Task DeactivateAsync(Guid userId);
}