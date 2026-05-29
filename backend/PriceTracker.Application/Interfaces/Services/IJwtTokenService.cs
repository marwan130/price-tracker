namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Domain.Entities;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid?  GetUserIdFromToken(string token);
}