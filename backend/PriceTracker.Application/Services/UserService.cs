namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Users;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
        => _userRepository = userRepository;

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

    private static UserResponse MapToResponse(User user) => new()
    {
        UserId    = user.UserId,
        Name      = user.Name,
        Email     = user.Email,
        Phone     = user.Phone,
        IsActive  = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}