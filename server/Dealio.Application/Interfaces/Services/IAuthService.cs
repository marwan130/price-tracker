namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Auth;

public interface IAuthService
{
    Task<AuthResponse>  RegisterAsync(RegisterRequest request);
    Task<AuthResponse>  LoginAsync(LoginRequest request);
    Task<AuthResponse>  LoginWithGoogleAsync(string credential);
    Task                VerifyEmailAsync(string token);
    Task                ResendVerificationEmailAsync(string email);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task                LogoutAsync(Guid userId, string refreshToken);
    Task                ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task                ForgotPasswordAsync(ForgotPasswordRequest request);
    Task                ResetPasswordAsync(ResetPasswordRequest request);
}
