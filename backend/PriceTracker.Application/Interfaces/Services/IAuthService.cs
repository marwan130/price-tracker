namespace PriceTracker.Application.Interfaces.Services;

using PriceTracker.Application.DTOs.Auth;

public interface IAuthService
{
    Task<AuthResponse>  RegisterAsync(RegisterRequest request);
    Task<AuthResponse>  LoginAsync(LoginRequest request);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task                LogoutAsync(string refreshToken);
    Task                ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}